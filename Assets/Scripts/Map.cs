using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class Map : MonoBehaviour
{
    // map will be tiled of type square
    public int size = 64;

    // the default possible tiles - they have to be of a set size
    public GameObject playerObject;
    public GameObject enemyObject;
    public GameObject wallTile;
    public GameObject wallEndTile;
    public GameObject wallCornerTile;
    public GameObject wallCornerBiTile;
    public GameObject wallCornerAllTile;
    public GameObject doorClosedTile;
    public GameObject doorOpenTile;
    public GameObject floorArrowTile;


    // the matrix
    private int[,] map = new int[258, 258];

    // map structure is made of a big corridor that goes from the bottom to the top, and other smaller corridors spawn from it
    // we save the x coordinate of where the previous' room main corridor ended in the variable below, so we can start our current main corridor from it
    private int lastMapCorridorX;
    private int corridorX; // x coordinate of our current corridor's end

    // initial corridor size (across)
    private int corridorIndex = 0;
    public int[] corridorSizes = new int[4] { 9, 5, 3, 1 };
    private int corridorSize; // gets values from the array above
    public float percentCorridors = 0.04f; // what percent of available spots for corridors the algorithm will try to turn into corridors
    public float doorRarity = 0.04f;

    private int testingHorizontal = 0;
    private int testingVertical = 0;

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
        corridorSize = corridorSizes[corridorIndex];

        // surrounding with walls
        size += 2; // increasing the size because we want to surround the map by walls
        for (int i = 0; i < size; i++)
        {
            map[0, i] = 1;
            map[size - 1, i] = 1;
            map[i, 0] = 1;
            map[i, size - 1] = 1;
        }

        // generating corridors
        lastMapCorridorX = size / 2; // first corridor will always start in the middle
        corridorX = Random.Range(1 + corridorSize / 2, size - corridorSize / 2 - 1);

        // creating the corridors on the map
        handleCorridors();

        // placing the player
        map[lastMapCorridorX, 2] = 50;

        // generating doors so all the rooms can be accessed
        generateDoors();

        // placing enemies
        for (int i = 1; i < size - 1; i++)
            for (int j = 1; j < size - 1; j++)
            {
                if (map[i, j] >= 100 || map[i, j] == 0)
                    if (Random.value <= 0.002f)
                        map[i, j] = 51; // enemy
            }

        // function which creates the game objects
        generateMap();

        AstarPath.active.Scan();
    }

    private void handleCorridors()
    {
        // corridor layer - starting with 100, for main corridor
        int layer = 100;

        // generating the main corridor
        generateCorridor(new Vector2Int(lastMapCorridorX, 0), new Vector2Int(corridorX, size - 1), layer, false, true);

        List<Vector2Int> options = new List<Vector2Int>();
        int p1, p2;

        while (corridorIndex < corridorSizes.Length - 1)
        {
            // scanning for options
            for (int i = 1; i < size - 1; i++)
                for (int j = 1; j < size - 1; j++)
                {
                    if (map[i, j] == 1) // only looking at walls
                    { 
                        // horizontal
                        p1 = map[i - 1, j];
                        p2 = map[i + 1, j];
                        if ((p1 == layer && p2 == 0) || (p1 == 0 && p2 == layer))
                            options.Add(new Vector2Int(i, j));
                        // vertical
                        p1 = map[i, j - 1];
                        p2 = map[i, j + 1];
                        if ((p1 == layer && p2 == 0) || (p1 == 0 && p2 == layer))
                            options.Add(new Vector2Int(i, j));
                    }
                }

            layer++;
            corridorSize = corridorSizes[++corridorIndex];

            int nrCorridors = (int)(options.Count * percentCorridors);
            List<Vector2Int> results = getCorridorOptions(options, nrCorridors);
            foreach (Vector2Int v in results)
            {
                bool horizontal = false;
                Vector2Int end = findCorridorEnd(v, ref horizontal);
                if (end.x != -1) // making sure we got a valid result
                {
                    if (horizontal)
                        testingHorizontal++;
                    else
                        testingVertical++;
                    generateCorridor(v, end, layer, horizontal, false);
                }
            }
        }

        Debug.Log(testingHorizontal + ": " + testingVertical);
    }

    Vector2Int findCorridorEnd(Vector2Int start, ref bool horizontal)
    {
        int x = start.x;
        int y = start.y;
        int direction = -1; // 0 = left, 1 = up, 2 = right, 3 = down

        // horizontal
        int left, right;
        left = map[x - 1, y]; // left
        right = map[x + 1, y]; // right
        if (left == 0 && right >= 100) // left
            direction = 0;
        if (right == 0 && left >= 100) // right
            direction = 2;

        // vertical
        int down, up;
        down = map[x, y - 1]; // down
        up = map[x, y + 1]; // up
        if (down == 0 && up >= 100) // down
            direction = 3;
        if (up == 0 && down >= 100) // up
            direction = 1;

        // choosing the end
        int newX = -1, newY = -1;
        if(direction == 0) // left
        {
            horizontal = true;
            newX = 0;
            newY = y + Random.Range(0, size / 5) - size / 10;
        }
        else if (direction == 2) // right
        {
            horizontal = true;
            newX = size - 1;
            newY = y + Random.Range(0, size / 5) - size / 10;
        }
        else if (direction == 1) // up
        {
            horizontal = false;
            newX = x + Random.Range(0, size / 5) - size / 10;
            newY = size - 1;
        }
        else if (direction == 3) // down
        {
            horizontal = false;
            newX = x + Random.Range(0, size / 5) - size / 10;
            newY = 0;
        }

        return new Vector2Int(newX, newY);
    }

    List<Vector2Int> getCorridorOptions(List<Vector2Int> original, int nr)
    {
        List<Vector2Int> options = new List<Vector2Int>();

        Vector2Int choice;
        int maxTries = 1000;
        int added = 0;
        while (maxTries > 0)
        {
            choice = original[Random.Range(0, original.Count)];
            if (farEnough(options, choice, 3 * (corridorSize)))
            {
                options.Add(choice);
                added++;
            }
            if (added >= nr)
                break;
            maxTries--;
        }

        return options;
    }

    bool farEnough(List<Vector2Int> vectors, Vector2Int choice, float minDistance)
    {
        foreach (Vector2Int v in vectors)
            if (Vector2Int.Distance(v, choice) < minDistance)
                return false;
        return true;
    }

    // we will walk a square around and fill the map with the layer number inside of it
    // and outside of the radius we will fill with walls, but only if we are not replacing a layer number - this way they are all connected and surrounded by walls
    private void generateCorridor(Vector2Int start, Vector2Int end, int layer, bool horizontal, bool stripeMode)
    {
        Vector2Int center = start;

        // setting the allowed move directions
        Vector2Int[] direction = new Vector2Int[2]; 
        direction[0] = new Vector2Int((int)Mathf.Sign(end.x - start.x), 0);
        direction[1] = new Vector2Int(0, (int)Mathf.Sign(end.y - start.y));
        int current = 0; // current direction
        if (!horizontal)
            current = 1;
        

        // setting the coordinate of the random change direction - because we're only moving up down left right, for diagonal paths we have to switch direction at some point
        int changeDirCoord;
        if (horizontal)
            changeDirCoord = Random.Range(Mathf.Min(start.x, end.x) + 2 * corridorSize, Mathf.Max(start.x, end.x) - 2 * corridorSize);
        else
            changeDirCoord = Random.Range(Mathf.Min(start.y, end.y) + 2 * corridorSize, Mathf.Max(start.y, end.y) - 2 * corridorSize);

        bool validMove = true;
        // for turning back after the direction change
        bool onAxis = false;
        if (center.x == end.x || center.y == end.y)
            onAxis = true;
        while(validMove)
        {
            // fill the square - we go one size over for the square because we want to include the walls as well
            int minBoundX = center.x - corridorSize / 2 - 1;
            int maxBoundX = center.x + corridorSize / 2 + 1;
            int minBoundY = center.y - corridorSize / 2 - 1;
            int maxBoundY = center.y + corridorSize / 2 + 1;
            for (int i = minBoundX; i <= maxBoundX; i++)
                for(int j = minBoundY; j <= maxBoundY; j++)
                {
                    if(i >= 0 && i < size && j >= 0 && j < size) // only check inside the map limits
                    {
                        if (i == minBoundX || i == maxBoundX || j == minBoundY || j == maxBoundY) // on the square bounds: we should place a wall
                        {
                            if (map[i, j] == 0) // only place if the spot is empty
                                map[i, j] = 1;
                        }
                        else // inside the square: we place the layer number, without ovewriting another layer
                        {
                            if(map[i, j] < 100) // current position is not taken by a previous layer
                            {
                                // dont overwrite map border walls - other walls OK
                                if (!(i == 0 || i == size - 1 || j == 0 || j == size - 1))
                                    map[i, j] = layer;
                            }
                        }
                    }
                }

            // change direction as per the coordinate calculated above
            if ((horizontal && center.x == changeDirCoord) || (!horizontal && center.y == changeDirCoord))
            {
                current = (current + 1) % 2;
                changeDirCoord = -1; // making sure we don't switch again
            }

            // change direction if we reached the endPoint's axis
            if(!onAxis && (center.x == end.x || center.y == end.y))
            {
                current = (current + 1) % 2;
                onAxis = true;
            }

            // determine if the next position is valid
            validMove = true;
            if (validPosition(center, direction[current], layer, end))
            {
                center += direction[current];
                if (stripeMode)
                {
                    float angle = 0;
                    Vector2Int dir = direction[current];
                    if (dir.x == -1)
                        angle = -90;
                    else if (dir.x == 1)
                        angle = 90;
                    Instantiate(floorArrowTile, new Vector3(center.x, center.y, 0), Quaternion.Euler(0, 0, -angle), transform);
                }
            }
            else
                validMove = false;
        }

    }

    bool validPosition(Vector2Int point, Vector2Int direction, int layer, Vector2Int end)
    {
        Vector2Int nextPos = point + direction;

        // if we reached one axis of the end point and we are currently on it, we cant leave it -> invalid move if we do
        if (point.x == end.x && nextPos.x != end.x)
            return false;
        if (point.y == end.y && nextPos.y != end.y)
            return false;

        int minBoundX = nextPos.x - corridorSize / 2 - 1;
        int maxBoundX = nextPos.x + corridorSize / 2 + 1;
        int minBoundY = nextPos.y - corridorSize / 2 - 1;
        int maxBoundY = nextPos.y + corridorSize / 2 + 1;
        for (int i = minBoundX; i <= maxBoundX; i++)
            for (int j = minBoundY; j <= maxBoundY; j++)
            {
                if (i >= 0 && i < size && j >= 0 && j < size) // only check inside the map limits
                {
                    // intersect another layer - exit
                    int xFuture = point.x + direction.x * (corridorSize / 2 + 1);
                    int yFuture = point.y + direction.y * (corridorSize / 2 + 1);
                    if (xFuture > 0 && xFuture < size - 1 && yFuture > 0 && yFuture < size - 1 && map[xFuture, yFuture] >= 100)
                        return false;
                }
            }
        return true;
    }


    // generates doors (if needed) such that all the rooms are accessible
    private void generateDoors()
    {
        // connect rooms step by step
        List<Vector2Int> doors = new List<Vector2Int>();
        int layer = 100;
        while (connectRoom(ref doors, layer))
        {
            // filling the room
            foreach(Vector2Int door in doors)
            {
                map[door.x, door.y] = 2; // creating the door
            }
            layer++;
        }

        // using pathfinding to make sure that all the rooms are accessible
        // first we have to clear the layer tags
        for (int i = 1; i < size - 1; i++)
            for (int j = 1; j < size - 1; j++)
                if (map[i, j] > 100)
                    map[i, j] = 0;
        // start pathfinding at player location
        fill(lastMapCorridorX, 2);
        // keep filling the rooms until none can be linked
        while (connectRoom(ref doors, 100))
        {
            // filling the room
            foreach (Vector2Int door in doors)
            {
                map[door.x, door.y] = 2; // creating the door
                fill(door.x, door.y);
            }
            layer++;
        }
    }

    // scans the map and chooses a random point out of the possible ones, in order to connect a room
    private bool connectRoom(ref List<Vector2Int> doorList, int layer)
    {
        List<Vector2Int> possibleLocations = new List<Vector2Int>();
        for (int i = 1; i < size - 1; i++)
            for (int j = 1; j < size - 1; j++)
            {
                // good location if on one axis the neighbours are both walls
                // and on the other axis there's a filled floor and an unfilled neighbour
                if (map[i - 1, j] == 1 && map[i + 1, j] == 1) // horizontal wall - x axis
                {
                    if (map[i, j - 1] == 0 && map[i, j + 1] == layer)
                        possibleLocations.Add(new Vector2Int(i, j));
                    else if (map[i, j - 1] == layer && map[i, j + 1] == 0)
                        possibleLocations.Add(new Vector2Int(i, j));
                } 
                else if (map[i, j - 1] == 1 && map[i, j + 1] == 1) // vertical wall - y axis
                {
                    if (map[i - 1, j] == 0 && map[i + 1, j] == layer)
                        possibleLocations.Add(new Vector2Int(i, j));
                    else if (map[i - 1, j] == layer && map[i + 1, j] == 0)
                        possibleLocations.Add(new Vector2Int(i, j));
                }
            }
        // randomly selecting a location
        if (possibleLocations.Count > 0)
        {
            doorList = getDoorOptions(possibleLocations, (int)(possibleLocations.Count * doorRarity));
            return true;
        }
        else
            return false; // found no possible positions
    }

    List<Vector2Int> getDoorOptions(List<Vector2Int> original, int nr)
    {
        List<Vector2Int> options = new List<Vector2Int>();

        Vector2Int choice;
        int maxTries = 1000;
        int added = 0;
        while (maxTries > 0)
        {
            choice = original[Random.Range(0, original.Count)];
            if (farEnough(options, choice, 5))
            {
                options.Add(choice);
                added++;
            }
            if (added >= nr)
                break;
            maxTries--;
        }

        return options;
    }

    private void fill(int x, int y)
    {
        // only continue if current position is floor, unfilled
        if (map[x, y] == 0 || map[x, y] == 2) // floor or door
        {
            if(map[x,y] != 2) // dont delete doors
                map[x, y] = 100;
            if (x < size - 2)
                fill(x + 1, y);
            if (x > 1) 
                fill(x - 1, y);
            if (y < size - 2) 
                fill(x, y + 1);
            if (y > 1)
                fill(x, y - 1);
        } // (unwritten) if it's a wall, we do nothing, we just exit the function
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
        for(int i = 0; i < size; i++)
            for(int j = 0; j < size; j++)
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
        if (j < size - 1 && map[i, j + 1] == 1) 
            ne[2] = 1;
        if (i < size - 1 && map[i + 1, j] == 1) 
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
    }

    private void adjustDoorTile(int i, int j, Vector3 newPos)
    {
        if (i > 0 && i < size - 1 && map[i - 1, j] == 1 && map[i + 1, j] == 1)
        {
            // vertical wall - need to rotate door tile 90 degrees            
            Instantiate(doorOpenTile, newPos, Quaternion.identity, transform);
            Instantiate(doorClosedTile, newPos, Quaternion.identity, transform);
        }
        else if (j > 0 && j < size - 1 && map[i, j - 1] == 1 && map[i, j + 1] == 1) 
        {
            Instantiate(doorOpenTile, newPos, Quaternion.Euler(0, 0, 90), transform);
            Instantiate(doorClosedTile, newPos, Quaternion.Euler(0, 0, 90), transform);
        }
    }
}
