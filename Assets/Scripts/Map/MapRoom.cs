using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// the MapRoom class is used when processing how the rooms of the map will be generated
public class MapRoom
{
    // the horizontal coordinates delimiting the room (in tiles)
    // so the room's rectangle shape starts from xLeft and goes up to xRight
    private int left;
    private int right;
    // same for vertical coordinates
    private int up;
    private int down;
    // list of doors - they have to be on the edges of the room
    private List<Vector2Int> doors;
    // minimum usable space in any room (no walls) --- so if it's 3, that means the smallest room space will be 3x3, with walls the final room will be 5x5
    public static int minRoomUsableLength = 3;
    // how many wall tiles in a wall for a door tile to be placed
    public static int wallsPerDoor = 7;
    // minimum distance between doors
    public static int doorMinDistance = 3;
    // chance when creating new corridor doors to make symmetric pairs
    public static float doorSymmetryChance = 0.4f;

    // the maximum room lengths for which the corridor will be of width (array index + 1)
    // Note: the length is reffering to the room which is being split, not the resulting one
    // so for {10, 20, 30}:
    //      rooms of width 1-10 will have corridors of width 0 - so just one wall separating them
    //      rooms of width 11-20 will have corridors of width 1
    //      rooms of width 21-30 will have corridors of width 2
    //      rooms of width 31+ will have corridors of width 3
    public static int[] roomToCorridorLengths = { 10, 25, 35, 50 };

    public int Left { get => left; set => left = value; }
    public int Right { get => right; set => right = value; }
    public int Up { get => up; set => up = value; }
    public int Down { get => down; set => down = value; }
    public List<Vector2Int> Doors { get => doors; set => doors = value; }

    public MapRoom(int left, int right, int up, int down)
    {
        this.left = left;
        this.right = right;
        this.up = up;
        this.down = down;
        doors = new List<Vector2Int>();
    }

    // checks whether we can make a vertical split (vertical corridor line)
    private bool CanSplitVertical()
    {
        // we must compute the minimum room width in order to split it, while taking into account <minRoomUsableLength>
        // we must double <minRoomUsableLength> since we will have two rooms, then add for each room 2 tiles for walls and 1 tile for the corridor between
        // for <minRoomUsableLength> = 3, it would look like this:

        //  W W W W W W W W W        W W W W W C W W W W W    W = wall
        //  W . . . W . . . W        W . . . W C W . . . W    C = corridor (optional)
        //  W . . . W . . . W   OR   W . . . W C W . . . W    . = free space
        //  W . . . W . . . W        W . . . W C W . . . W    (doors added later)
        //  W W W W W W W W W        W W W W W C W W W W W    

        int roomWidth = GetRoomWidth();
        int corridorWidth = ComputeCorridorWidth(roomWidth);

        int wallTiles = 4; // 2 exterior walls and 2 corridor walls
        if (corridorWidth == 0)
            wallTiles = 3; // 2 exterior walls and 1 wall without corridor

        int minLength = (minRoomUsableLength * 2) + wallTiles + corridorWidth;

        if (GetRoomWidth() >= minLength)
            return true;
        return false;
    }

    // checks whether we can make a horizontal split (horizontal corridor line)
    private bool CanSplitHorizontal()
    {
        // same logic as found in CanSplitVertical()
        int roomHeight = GetRoomWidth();
        int corridorWidth = ComputeCorridorWidth(roomHeight);

        int wallTiles = 4; // 2 exterior walls and 2 corridor walls
        if (corridorWidth == 0)
            wallTiles = 3; // 2 exterior walls and 1 wall without corridor

        int minLength = (minRoomUsableLength * 2) + wallTiles + corridorWidth;

        if (GetRoomHeight() >= minLength)
            return true;
        return false;
    }

    private bool ComputeSplitDirection()
    {
        bool vertical = false;
        if (CanSplitHorizontal())
        {
            if (CanSplitVertical()) // if we can split both ways, we randomly choose
            {
                // we choose based on the height and width, so if a room is 6 tiles wide and 4 tiles high, it has 60% chance to split vertically and 40% horizontally
                float verticalChance = (1.0f * GetRoomWidth() / (GetRoomWidth() + GetRoomHeight()));
                float horizonalChance = (1.0f * GetRoomHeight() / (GetRoomWidth() + GetRoomHeight()));
                float exponent = 3;
                verticalChance = Mathf.Pow(verticalChance, exponent);
                horizonalChance = Mathf.Pow(horizonalChance, exponent);
                float vVal = Random.value * verticalChance;
                float hVal = Random.value * horizonalChance;
                //Debug.Log("VerticalChance: " + verticalChance);
                vertical = (vVal > hVal);
            }
        }
        else
        {
            if (CanSplitVertical())
                vertical = true;
        }
        return vertical;
    }

    // returns a corridor width in relation with the room wall width using the values from <roomToCorridorLengths>
    // even though its called width it should not be confused to the room width (it's how "thick" the corridor line is)
    // <roomLength> refers to either height or width, depending on where it's used
    private int ComputeCorridorWidth(int roomLength)
    {
        for (int i = 0; i < roomToCorridorLengths.Length; i++)
        {
            if (roomLength <= roomToCorridorLengths[i])
                return i;
        }
        return roomToCorridorLengths.Length;
    }

    // assigns doors from the initial room to the new room(s) with respect of the room's bounds
    private MapRoom AssignDoors(MapRoom r, List<Vector2Int> doors)
    {
        List<Vector2Int> newDoors = new List<Vector2Int>();
        for (int i = 0; i < doors.Count; i++)
        {
            Vector2Int d = doors[i];
            // we don't have to check for doors being placed in the corners because we never will (see CheckCorridorDoorConflicts())
            if (d.x >= left && d.x <= right && d.y >= up && d.y <= down)
                newDoors.Add(d);
        }
        r.Doors = newDoors;
        return r;
    }

    // checks if a door is too close to another door
    // returns true if no conflicts
    public bool CheckDoorConflicts(Vector2Int d, bool vertical)
    {
        foreach (var ed in doors)
        {
            if (vertical && ed.x == d.x && Mathf.Abs(d.y - ed.y) < doorMinDistance)
                return false;
            if (!vertical && ed.y == d.y && Mathf.Abs(d.x - ed.x) < doorMinDistance)
                return false;
        }
        return true;
    }

    public void AddDoor(Vector2Int d)
    {
        this.doors.Add(d);
    }

    // generates new doors along the walls of the newly formed corridor
    // returns the 2 new completed rooms
    private List<MapRoom> GenerateCorridorDoors(MapRoom r1, MapRoom r2, bool vertical)
    {
        int minCoord, maxCoord;
        if (vertical)
        {
            minCoord = r1.Up + 1;
            maxCoord = r1.Down - 1;
        }
        else
        {
            minCoord = r1.Left + 1;
            maxCoord = r1.Right - 1;
        }

        // how many doors we have to place per room in order to complete the operation
        int doorsToDo = Mathf.Max(Mathf.RoundToInt((maxCoord - minCoord + 1) / 1.0f / wallsPerDoor), 1);
        while(doorsToDo > 0)
        {
            List<Vector2Int> possibleR1 = new List<Vector2Int>(); // possible coordinates for doors in R1
            List<Vector2Int> possibleR2 = new List<Vector2Int>(); // possible coordinates for doors in R2
            List<List<Vector2Int>> possibleBoth = new List<List<Vector2Int>>(); // possible symmetrical pairs of doors for both rooms (door to door across the corridor)
            // adding all the possible options in lists
            for (int i = minCoord; i <= maxCoord; i++)
            {
                Vector2Int r1Door, r2Door;
                if (vertical)
                {
                    r1Door = new Vector2Int(r1.Right, i);
                    r2Door = new Vector2Int(r2.Left, i);
                }
                else
                {
                    r1Door = new Vector2Int(i, r1.Down);
                    r2Door = new Vector2Int(i, r2.Up);
                }
                bool placedR1 = false;
                if (r1.CheckDoorConflicts(r1Door, vertical))
                {
                    possibleR1.Add(r1Door);
                    placedR1 = true;
                }
                if (r2.CheckDoorConflicts(r2Door, vertical))
                {
                    possibleR2.Add(r2Door);
                    if (placedR1) // check if possible for symmetry
                    {
                        List<Vector2Int> l = new List<Vector2Int>();
                        l.Add(r1Door);
                        l.Add(r2Door);
                        possibleBoth.Add(l);
                    }
                }
            }

            // exit flag for when we can't place ANY doors
            bool zeroOptions = true; 
            // roll for symmetry and check if we have any possible pairs
            if (possibleBoth.Count > 0 && Random.value <= doorSymmetryChance)
            {
                List<Vector2Int> val = possibleBoth[Random.Range(0, possibleBoth.Count)];
                r1.AddDoor(val[0]);
                r2.AddDoor(val[1]);
                doorsToDo--;
                zeroOptions = false;
            }
            else
            {
                // to make sure that we don't decrease <doorsToDo> twice (since we will consider just one door for R1 enough even if we can't place one for R2 (and the other way around))
                bool decreased = false;
                if(possibleR1.Count > 0)
                {
                    Vector2Int val = possibleR1[Random.Range(0, possibleR1.Count)];
                    r1.AddDoor(val);
                    doorsToDo--;
                    decreased = true;
                    zeroOptions = false;
                }
                if (possibleR2.Count > 0)
                {
                    Vector2Int val = possibleR2[Random.Range(0, possibleR2.Count)];
                    r2.AddDoor(val);
                    if(!decreased)
                        doorsToDo--;
                    zeroOptions = false;
                }
            }
            // exit if we can't place any more doors
            if (zeroOptions)
                break;
        }

        return new List<MapRoom>() { r1, r2 };
    }

    // generates new doors on the single wall that was placed between the rooms
    // it's basically a simplified version of GenerateCorridorDoors()
    // returns the 2 new completed rooms
    private List<MapRoom> GenerateSingleWallDoors(MapRoom r1, MapRoom r2, bool vertical)
    {
        
        int minCoord, maxCoord;
        if (vertical)
        {
            minCoord = r1.Up + 1;
            maxCoord = r1.Down - 1;
        }
        else
        {
            minCoord = r1.Left + 1;
            maxCoord = r1.Right - 1;
        }

        // how many doors we have to place per room in order to complete the operation
        int doorsToDo = Mathf.Max(Mathf.RoundToInt((maxCoord - minCoord + 1) / 1.0f / wallsPerDoor), 1);
        while (doorsToDo > 0)
        {
            List<List<Vector2Int>> possibleDoors = new List<List<Vector2Int>>(); // possible coordinates for doors in R1
            // adding all the possible options in lists
            for (int i = minCoord; i <= maxCoord; i++)
            {
                Vector2Int doorR1, doorR2;
                if (vertical)
                {
                    doorR1 = new Vector2Int(r1.Right, i);
                    doorR2 = new Vector2Int(r2.Left, i);
                }
                else
                {
                    doorR1 = new Vector2Int(i, r1.Down);
                    doorR2 = new Vector2Int(i, r2.Up);
                }
                if (r1.CheckDoorConflicts(doorR1, vertical) && r2.CheckDoorConflicts(doorR2, vertical))
                {
                    List<Vector2Int> doorPair = new List<Vector2Int>();
                    doorPair.Add(doorR1);
                    doorPair.Add(doorR2);
                    possibleDoors.Add(doorPair);
                }
            }

            // exit flag for when we can't place ANY doors
            bool zeroOptions = true;
            if (possibleDoors.Count > 0)
            {
                List<Vector2Int> val = possibleDoors[Random.Range(0, possibleDoors.Count)];
                r1.AddDoor(val[0]);
                r2.AddDoor(val[1]);
                doorsToDo--;
                zeroOptions = false;
            }
            // exit if we can't place any more doors
            if (zeroOptions)
                break;
        }
        return new List<MapRoom>() { r1, r2 };
    }

    // tries to place corridors such that no doors are overwritten
    private List<int> CheckCorridorDoorConflicts(int minCoord, int maxCoord, int corridorWidth, bool vertical)
    {
        List<int> ret = new List<int>(); // we store the valid coordinates here
        for (int coord = minCoord; coord <= maxCoord; coord++)
        {
            bool valid = true;
            foreach (var d in doors){
                if (vertical){
                    foreach (var p in new int[] { up, down }){ // one point for each wall the corridor touches
                        if (d.y == p){ // the point is on the same wall as the door
                            if (corridorWidth == 0) // single wall
                            {
                                if (coord == d.x) // wall hits door
                                {
                                    valid = false;
                                    break;
                                }
                            }
                            else // corridor surrounded by walls
                            {
                                if ((d.x - coord <= corridorWidth) && (coord - d.x <= 1)) // we overwrite the door going right/down
                                {
                                    valid = false;
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    foreach (var p in new int[] { left, right }) { // one point for each wall the corridor touches
                        if (d.x == p) { // the point is on the same wall as the door
                            if (corridorWidth == 0) // single wall
                            {
                                if (coord == d.y) // wall hits door
                                {
                                    valid = false;
                                    break;
                                }
                            }
                            else // corridor surrounded by walls
                            {
                                if ((d.y - coord <= corridorWidth) && (coord - d.y <= 1)) // we overwrite the door going right/down
                                {
                                    valid = false;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            if(valid)
                ret.Add(coord);
        }
        return ret;
    }

    // splits the room into two separate rooms
    // can also be used as a check, as if it returns null, it means the room can't be split further
    public List<MapRoom> Split()
    {
        // we chech if we can do any split by length
        if (!CanSplitHorizontal() && !CanSplitVertical())
            return null;

        // we determine the orientation of the split
        bool vertical = ComputeSplitDirection();

        // we compute the length of the side where the corridor will "touch" --- needed to determine a corridor width
        int splitWallLength;
        int minCoord, maxCoord; // take the values of (left, right) or (up, down) depending on orientation
        if (vertical)
        {
            minCoord = left; maxCoord = right;
            splitWallLength = down - up + 1;
        }
        else
        {
            minCoord = up; maxCoord = down;
            splitWallLength = right - left + 1;
        }
        // now we determine how wide should the corridor be
        int corridorWidth = ComputeCorridorWidth(splitWallLength);
        int wallTiles = 2; // 2 walls on either side of the corridor
        if (corridorWidth == 0)
            wallTiles = 1; // just one wall and no corridor

        // formula for determining the starting coordinate on the corridor (corridor width taken into account)
        int minPossibleCoordinate = minCoord + minRoomUsableLength + wallTiles;
        int maxPossibleCoordinate = maxCoord - minRoomUsableLength - wallTiles - (corridorWidth - 1);
        if (corridorWidth == 0) // need to adjust formula if we have no corridor, just a wall
            maxPossibleCoordinate--;

        // checking for any door conflicts getting a random value from the validated coordinates
        List<int> possibleCoordinates = CheckCorridorDoorConflicts(minPossibleCoordinate, maxPossibleCoordinate, corridorWidth, vertical);
        int corridorMinCoord;
        if (possibleCoordinates.Count > 0)
            corridorMinCoord = possibleCoordinates[Random.Range(0, possibleCoordinates.Count)]; // randomly picking one of the valid options
        else
            return null; // we can't split the room

        // getting the two new rooms
        MapRoom r1, r2;
        if (vertical)
        {
            r1 = new MapRoom(left, corridorMinCoord - (wallTiles - 1), up, down);
            r2 = new MapRoom(corridorMinCoord + corridorWidth, right, up, down);
        }
        else
        {
            r1 = new MapRoom(left, right, up, corridorMinCoord - (wallTiles - 1));
            r2 = new MapRoom(left, right, corridorMinCoord + corridorWidth, down);
        }

        // copying the old doors over
        r1 = AssignDoors(r1, doors);
        r2 = AssignDoors(r2, doors);

        // generating new doors along the corridor walls
        if (corridorWidth > 0)
            return GenerateCorridorDoors(r1, r2, vertical);
        else
            return GenerateSingleWallDoors(r1, r2, vertical);
    }

    public int GetRoomWidth()
    {
        return right - left + 1;
    }

    public int GetRoomHeight()
    {
        return down - up + 1;
    }

    public int GetRoomArea()
    {
        return GetRoomHeight() * GetRoomWidth();
    }
}
