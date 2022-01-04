using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using System.Linq;

public class Map : MonoBehaviour
{
    // map structure is made of a rooms and corridors between them - all of them on a grid of tiles
    // after that, we split the halves down into smaller rooms by adding new corridors
    // once a room is selected, it is split in 3: the corridor itself, and the 2 new resulting rooms on either side
    // doors are placed randomly on the new corridor, within some parameters (nr doors, min distance between doors etc.)
    // a room cannot be split if it is too small (either area, or height or width, or all 3 combined)
    // we want to have diverse rooms, so let's say we make a few categories based on size that we want in the final map
    // once those categories are deemed sufficient, the splitting ends
    // finally we place a few exits so the player can go to a new map

    // the default possible tiles - prefabs of a set size
    public GameObject playerObject;
    public GameObject enemyObject;
    public GameObject wallTile;
    public GameObject wallEndTile;
    public GameObject wallCornerTile;
    public GameObject wallCornerBiTile;
    public GameObject wallCornerAllTile;
    public GameObject wallEndAll;
    public GameObject doorClosedTile;
    public GameObject doorOpenTile;
    public GameObject floorArrowTile;

    // -------------------- MapRoom parameters ----------------------
    // minimum usable space in any room (no walls) --- so if it's 3, that means the smallest room space will be 3x3, with walls the final room will be 5x5
    public int minRoomUsableLength = 3;
    // how many wall tiles in a wall for a door tile to be placed
    public int wallsPerDoor = 3;
    // minimum distance between doors
    public int doorMinDistance = 9;
    // chance when creating new corridor doors to make symmetric pairs
    public float doorSymmetryChance = 1f;

    // the maximum room lengths for which the corridor will be of width (array index + 1)
    // Note: the length is reffering to the room which is being split, not the resulting one
    // so for {10, 20, 30}:
    //      rooms of width 1-10 will have corridors of width 1
    //      rooms of width 11-20 will have corridors of width 2
    //      rooms of width 21-30 will have corridors of width 3
    //      rooms of width 31+ will have corridors of width 4
    public int[] roomToCorridorLengths = { 20, 35, 50 };
    // ---------------- End of MapRoom parameters ------------------



    // map will be tiled of type square
    public int mapSize = 64;
    // room divisions
    public int roomDivisions = 50;
    // the matrix
    private int[,] map = new int[258, 258];
    // Values:
    //      0 means floor
    //      1 means wall - could be normal or corner wall - we determine at the end
    //      2 means door
    //      50 means player
    //      51 means enemy
    //      99 means filled room (for door generating algorithm)
    //      100+ are layer markers - used both for generating the corridors and for pathfinding the rooms - they still represent floor tiles

    // Start is called before the first frame update
    void Start()
    {
        // MapRoom fields handling
        MapRoom.minRoomUsableLength = minRoomUsableLength;
        MapRoom.wallsPerDoor = wallsPerDoor;
        MapRoom.doorMinDistance = doorMinDistance;
        MapRoom.doorSymmetryChance = doorSymmetryChance;
        MapRoom.roomToCorridorLengths = roomToCorridorLengths;

        // map room handling
        List<MapRoom> rooms = new List<MapRoom>();
        rooms.Add(new MapRoom(0, mapSize - 1, 0, mapSize - 1));

        for (int i = 0; i < roomDivisions; i++)
        {
            rooms.Sort((r1, r2) => -r1.GetRoomArea().CompareTo(r2.GetRoomArea()));
            for (int j = 0; j < rooms.Count; j++)
            {
                List <MapRoom> newRooms = rooms[j].Split();
                if (newRooms != null)
                {
                    MapRoom oldRoom = rooms[j];
                    rooms.RemoveAt(j);
                    rooms.Add(newRooms[0]);
                    rooms.Add(newRooms[1]);
                    break;
                }
            }
        }

        // setting up the matrix
        for (int i = 0; i < mapSize; i++)
            for (int j = 0; j < mapSize; j++)
                map[i, j] = 0;

        // writing the rooms
        foreach (var r in rooms)
        {
            for(int x = r.Left; x <= r.Right; x++)
            {
                map[x, r.Up] = 1;
                map[x, r.Down] = 1;
            }

            for (int y = r.Up; y <= r.Down; y++)
            {
                map[r.Left, y] = 1;
                map[r.Right, y] = 1;
            }

            if (r.Doors != null)
            {
                foreach (var d in r.Doors)
                {
                    map[d.x, d.y] = 2;
                }
            }
        }

        // Re-filling the map edges
        for (int x = 0; x < mapSize; x++)
        {
            map[x, 0] = 1;
            map[x, mapSize - 1] = 1;
        }
        for (int y = 0; y <= mapSize; y++)
        {
            map[0, y] = 1;
            map[mapSize - 1, y] = 1;
        }

        // placing the player
        map[2, 2] = 50;

        /*
        // placing enemies
        for (int i = 1; i < mapSize - 1; i++)
            for (int j = 1; j < mapSize - 1; j++)
            {
                if (map[i, j] >= 100 || map[i, j] == 0)
                    if (Random.value <= 0.002f)
                        map[i, j] = 51; // enemy
            }
        */
        // function which creates the game objects
        generateMap();

        AstarPath.active.Scan();
    }

    // function which creates the game objects for the tiles
    //      0 means floor
    //      1 means wall - could be normal or corner wall - we determine at the end
    //      2 means door
    //      50 means player
    //      51 means enemy
    //      99 means filled room (for door generating algorithm)
    //      100+ are layer markers - used both for generating the corridors and for pathfinding the rooms - they still represent floor tiles
    private void generateMap()
    {
        for(int i = 0; i < mapSize; i++)
            for(int j = 0; j < mapSize; j++)
            {
                Vector3 newPos = new Vector3(i, j, 0);
                switch (map[i,j])
                {
                    case 1: // wall
                        adjustWallTile(i, j, newPos);
                        break;
                    case 2: // door
                        adjustDoorTile(i, j, newPos);
                        break;
                    case 50: // player
                        playerObject.transform.position = newPos;
                        break;
                    case 51: // enemy
                        Instantiate(enemyObject, newPos, Quaternion.identity, transform);
                        break;
                }
            }
    }

    private void adjustWallTile(int i, int j, Vector3 newPos)
    {
        // neighbours: 0 down, 1 left, 2 up, 3 right
        int[] ne = new int[4] { 0, 0, 0, 0 };
        if (j > 0 && map[i, j - 1] == 1)
            ne[0] = 1;
        if (i > 0 && map[i - 1, j] == 1)
            ne[1] = 1;
        if (j < mapSize - 1 && map[i, j + 1] == 1) 
            ne[2] = 1;
        if (i < mapSize - 1 && map[i + 1, j] == 1) 
            ne[3] = 1;


        // now if we find 2 consecutive ones in the array, we have a corner
        // if we find three, we have a bilateral corner, and 4 means all around wall
        // if we find just alternating 1 and 0, it means normal wall
        bool normal = false;
        bool corner = false;
        bool bi = false;
        bool all = false;
        int count = 0;
        for (int c = 0; c < 4; c++)
        {
            if (ne[c] == 1)
            {
                count++;
                if (ne[(c + 1) % 4] == 1)
                {
                    corner = true;
                    if (ne[(c + 2) % 4] == 1)
                    {
                        bi = true;
                    }
                }

            }
        }
        // normal wall
        if (ne[0] == 1 && ne[1] == 0 && ne[2] == 1 && ne[3] == 0)
            normal = true;
        if (ne[0] == 0 && ne[1] == 1 && ne[2] == 0 && ne[3] == 1)
            normal = true;

        // surrounded on all sides
        if (count == 4)
            all = true;

        if (all)
        {
            Instantiate(wallCornerAllTile, newPos, Quaternion.identity, transform);
            return;
        }
        if (bi)
        {
            for (int c = 0; c < 4; c++) 
                if(ne[c] == 0)
                {
                    float angle = c * 90 + 180;
                    if (angle >= 360)
                        angle -= 360;
                    Instantiate(wallCornerBiTile, newPos, Quaternion.Euler(0, 0, -angle), transform);
                    return;
                }
            return;
        }
        if (corner)
        {
            for (int c = 0; c < 4; c++)
                if (ne[c] == 1 && ne[(c + 1) % 4] == 1)
                {
                    float angle = c * 90 + 90;
                    Instantiate(wallCornerTile, newPos, Quaternion.Euler(0, 0, -angle), transform);
                    return;
                }
            return;
        }
        if (normal)
        {
            if (ne[0] == 1)
                Instantiate(wallTile, newPos, Quaternion.identity, transform);
            else
                Instantiate(wallTile, newPos, Quaternion.Euler(0, 0, 90), transform);
            return;
        }
        if(count == 1)
        {
            for (int c = 0; c < 4; c++)
                if (ne[c] == 1)
                {
                    float angle = c * 90;
                    Instantiate(wallEndTile, newPos, Quaternion.Euler(0, 0, -angle), transform);
                    return;
                }
        }
        // none matched, which means that the wall is between two doors, either straight or corner
        Instantiate(wallEndAll, newPos, Quaternion.identity, transform);
    }

    private void adjustDoorTile(int i, int j, Vector3 newPos)
    {
        if (i > 0 && i < mapSize - 1 && map[i - 1, j] == 1 && map[i + 1, j] == 1)
        {
            // vertical wall - need to rotate door tile 90 degrees            
            Instantiate(doorOpenTile, newPos, Quaternion.identity, transform);
            Instantiate(doorClosedTile, newPos, Quaternion.identity, transform);
        }
        else if (j > 0 && j < mapSize - 1 && map[i, j - 1] == 1 && map[i, j + 1] == 1) 
        {
            Instantiate(doorOpenTile, newPos, Quaternion.Euler(0, 0, 90), transform);
            Instantiate(doorClosedTile, newPos, Quaternion.Euler(0, 0, 90), transform);
        }
    }
}
