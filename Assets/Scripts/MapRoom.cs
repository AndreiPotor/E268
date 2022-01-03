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
    //      rooms of width 1-10 will have corridors of width 1
    //      rooms of width 11-20 will have corridors of width 2
    //      rooms of width 21-30 will have corridors of width 3
    //      rooms of width 31+ will have corridors of width 4
    public static int[] roomToCorridorLengths = { 20, 35, 50 };

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

    // a public function which checks whether or not the room is too small to split into two
    public bool CanSplit()
    {
        // either the height or the width has to be greater or equal than <minLength> in order to split
        if (CanSplitVertical() || CanSplitHorizontal())
            return true;
        return false;
    }

    // checks whether we can make a vertical split (vertical corridor line)
    private bool CanSplitVertical()
    {
        // we must compute the minimum room width in order to split it, while taking into account <minRoomUsableLength>
        // we must double <minRoomUsableLength> since we will have two rooms, then add for each room 2 tiles for walls and 1 tile for the corridor between
        // for <minRoomUsableLength> = 3, it would look like this:

        // W W W W W C W W W W W    W = wall
        // W . . . W C W . . . W    C = corridor
        // W . . . W C W . . . W    . = free space
        // W . . . W C W . . . W    (doors added later)
        // W W W W W C W W W W W

        int minLength = (minRoomUsableLength * 2) + 4 + 1;
        if (GetRoomWidth() >= minLength)
            return true;
        return false;
    }

    // checks whether we can make a horizontal split (horizontal corridor line)
    private bool CanSplitHorizontal()
    {
        // same principles as <CanSplitVertical()>
        int minLength = (minRoomUsableLength * 2) + 4 + 1;
        if (GetRoomHeight() >= minLength)
            return true;
        return false;
    }

    private bool ComputeSplitDirection()
    {
        bool vertical;
        if (CanSplitHorizontal())
        {
            if (CanSplitVertical()) // if we can split both ways, we randomly choose
            {
                // we choose based on the height and width, so if a room is 6 tiles wide and 4 tiles high, it has 60% chance to split vertically and 40% horizontally
                float verticalChance = (1.0f * GetRoomWidth() / (GetRoomWidth() + GetRoomHeight()));
                Debug.Log("VerticalChance: " + verticalChance);
                vertical = (Random.value <= verticalChance);
            }
            else // we can only split horizontally
                vertical = false;
        }
        else
        {
            if (CanSplitVertical())
            {
                vertical = true;
            }
            else
            { // can't split either way - should never reach here
                vertical = false;
                Debug.LogError("Error trying to split the room. Class MapRoom, function ComputeSplitDirection().");
            }
        }
        return vertical;
    }

    // returns a corridor width in relation with the room wall width using the values from <roomToCorridorLengths>
    private int ComputeCorridorWidth(int roomWidth)
    {
        for (int i = 0; i < roomToCorridorLengths.Length; i++)
        {
            if (roomWidth <= roomToCorridorLengths[i])
                return i + 1;
        }
        return roomToCorridorLengths.Length + 1;
    }

    // assigns correct doors with respect of the room's bounds
    private MapRoom AssignDoors(MapRoom r, List<Vector2Int> doors)
    {
        List<Vector2Int> newDoors = new List<Vector2Int>();
        for (int i = 0; i < doors.Count; i++)
        {
            Vector2Int d = doors[i];
            if (d.x > left && d.x < right && d.y > up && d.y < down)
                newDoors.Add(d);
        }
        r.Doors = newDoors;
        return r;
    }

    public bool CanPlaceDoor(Vector2Int d, bool vertical)
    {
        foreach (var ed in doors)
        {
            if (vertical && Mathf.Abs(d.y - ed.y) < doorMinDistance)
                return false;
            if (! vertical && Mathf.Abs(d.x - ed.x) < doorMinDistance)
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
    private List<MapRoom> GenerateDoors(MapRoom r1, MapRoom r2, bool vertical)
    {
        int doorsToDo;
        int minCoord, maxCoord;
        if (vertical)
        {
            minCoord = r1.Up;
            maxCoord = r1.Down;
        }
        else
        {
            minCoord = r1.Left;
            maxCoord = r1.Right;
        }

        // how many doors we have to place per room in order to complete the operation
        doorsToDo = Mathf.Max(Mathf.RoundToInt((maxCoord - minCoord + 1) / 1.0f / wallsPerDoor), 1);
        while(doorsToDo > 0)
        {
            Debug.Log("doorsToDo: " + doorsToDo);
            Debug.Log("minCoord: " + minCoord);
            Debug.Log("maxCoord: " + maxCoord);
            List<Vector2Int> possibleR1 = new List<Vector2Int>(); // possible coordinates for doors in R1
            List<Vector2Int> possibleR2 = new List<Vector2Int>(); // possible coordinates for doors in R2
            List<List<Vector2Int>> possibleBoth = new List<List<Vector2Int>>(); // possible symmetrical pairs of doors for both rooms (door to door across the corridor)
            // adding all the possible options in lists
            for (int i = minCoord + 1; i <= maxCoord - 1; i++)
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
                if (r1.CanPlaceDoor(r1Door, vertical))
                {
                    possibleR1.Add(r1Door);
                    placedR1 = true;
                }
                if (r2.CanPlaceDoor(r2Door, vertical))
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
            
            //Debug.Log("possibleR1:");
            if (possibleR1.Count == 0)
                Debug.Log("EMPTY LIST");
            /*
            foreach (var i in possibleR1)
            {
                Debug.Log(i);
            }
            Debug.Log("possibleR2:");
            foreach (var i in possibleR2)
            {
                Debug.Log(i);
            }
            Debug.Log("possibleBoth:");
            foreach (var i in possibleBoth)
            {
                Debug.Log(i[0]);
                Debug.Log(i[1]);
            }
            */
            

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
            {
                Debug.Log("FORCED EXIT");
                break;
            }
        }

        return new List<MapRoom>() { r1, r2 };
    }

    public List<MapRoom> Split()
    {
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

        // formula for determining the starting coordinate on the wall
        int corridorMinCoord = Random.Range(minCoord + minRoomUsableLength + 2, maxCoord - minRoomUsableLength - 2 - (corridorWidth - 1) + 1);

        // getting the two new rooms
        MapRoom r1, r2;
        if (vertical)
        {
            r1 = new MapRoom(left, corridorMinCoord - 1, up, down);
            r2 = new MapRoom(corridorMinCoord + corridorWidth, right, up, down);
        }
        else
        {
            r1 = new MapRoom(left, right, up, corridorMinCoord - 1);
            r2 = new MapRoom(left, right, corridorMinCoord + corridorWidth, down);
        }

        // copying the old doors over
        r1 = AssignDoors(r1, doors);
        r2 = AssignDoors(r2, doors);

        // generating new doors along the corridor walls
        List<MapRoom> ret = GenerateDoors(r1, r2, vertical);

        // return the 2 new rooms
        return ret;
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
