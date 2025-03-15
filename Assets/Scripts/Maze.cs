using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public enum MazeAlgorithm
{
    Prims,
    Kruskals,
    Wilsons,
    RecursiveBacktracker,
    AldousBroder
}

public class Maze : MonoBehaviour
{
    public const int OUT_OF_BOUNDS = -1;
    public const int WALL = 0;
    public const int PATH = 1;
    public const int DOOR = 2;

    public ControllerInput player;

    public int level = 1;
    public int round = 1;

    [Header("Maze Generation Settings")]
    public MazeAlgorithm algorithm;
    public int seed;
    public int width, height;

    public int portalPairsCount = 1;
    public int forceFieldCount = 8;

    private int[] leftEdges;
    private int[] bottomEdges;

    [SerializeField] private Vector2Int startCell;
    [SerializeField] private Vector2Int endCell;
    
    [Header("Rooms")]
    public int randomRoomCount;
    public int minRoomSize;
    public int maxRoomSize;
    public RectInt[] rooms;

    [Header("Route")]
    public MeshFilter route;
    public float routeGirth = 0.1f;


    [Header("Prefabs")]
    public GameObject wallPrefab;
    public GameObject doorPrefab;
    public GameObject floorPrefab;
    public GameObject floorStonesPrefab;

    public GameObject pillarPrefab;
    public GameObject pillarCornerPrefab;

    // Level 2
    public Portal portalPrefab;
    public ForceField forceFieldPrefab;

    public GameObject endCellPrefab;

    [ContextMenu("Restart")]
    public void Restart()
    {
        Debug.Log("Restarting maze");
        
        player.gameObject.SetActive(false);
        Rope rope = player.GetComponent<Rope>();
        rope.DestroyRope();

        seed = System.DateTime.Now.Millisecond;

        Random.InitState(seed);
        GenerateRandomRooms();
        GenerateMaze();
        startCell = RandomCell;
        endCell = RandomCell;
        InstantiatePrefabs();
        MovePlayer(startCell);
        portalOcclusionVolume.portals.Clear();

        if (level >= 2)
            InstantiateLevelTwoPrefabs();

        //rope.ConnectStartTo(new Vector3(startCell.x, 0.1f, startCell.y));
        player.gameObject.SetActive(true);
    }

    public enum Direction 
    {
        Forward,
        Right,
        Backward,
        Left,
    }

    [SerializeField] private PortalOcclusionVolume portalOcclusionVolume;

    public static Direction RandomDirection => (Direction)Random.Range(0, 4);

    public static Vector2Int AdjacentCell(Vector2Int cell, Direction direction)
    {
        switch (direction)
        {
            case Direction.Forward: return cell + Vector2Int.down;
            case Direction.Backward: return cell + Vector2Int.up;
            case Direction.Left: return cell + Vector2Int.right;
            case Direction.Right: return cell + Vector2Int.left;
            default: throw new System.Exception("Invalid direction");
        }
    }

    [ContextMenu("Instantiate Level Two Prefabs")]
    public void InstantiateLevelTwoPrefabs()
    {
        HashSet<Vector2Int> invalidCells = new HashSet<Vector2Int>
        {
            startCell,
            endCell
        };

        bool ValidPortalPlacement(Vector2Int cell_1, Direction dir_1, Vector2Int cell_2, Direction dir_2)
        {
            if (cell_1.x < 0 || cell_1.x >= width || cell_1.y < 0 || cell_1.y >= height)
                return false;

            if (cell_1 == cell_2)
                return false;

            if (invalidCells.Contains(cell_1) || invalidCells.Contains(cell_2))
                return false;

            if (IsPath(cell_1, AdjacentCell(cell_1, dir_1)))
                return false;

            if (IsPath(cell_2, AdjacentCell(cell_2, dir_2)))
                return false;

            if (GetEdgeType(cell_1, AdjacentCell(cell_1, dir_1)) != WALL)
                return false;

            if (GetEdgeType(cell_2, AdjacentCell(cell_2, dir_2)) != WALL)
                return false;

            //Debug.Log($"Valid portal placement: {cell_1} {dir_1} {cell_2} {dir_2}");

            return true;

        }

        bool ValidForceFieldPlacement(Vector2Int cell)
        {
            if (cell.x < 0 || cell.x >= width || cell.y < 0 || cell.y >= height)
                return false;

            if (invalidCells.Contains(cell))
                return false;

            return true;
        }

        portalOcclusionVolume.portals.Clear();

        for (int i = 0; i < portalPairsCount; i++)
        {
            int tries = 0;

            Vector2Int cell_1 = RandomCell;
            Vector2Int cell_2 = RandomCell;
            Direction dir_1 = RandomDirection;
            Direction dir_2 = RandomDirection;

            while (tries < 100)
            {
                if (!ValidPortalPlacement(cell_1, dir_1, cell_2, dir_2))
                {
                    tries++;
                    cell_1 = RandomCell;
                    cell_2 = RandomCell;
                    dir_1 = RandomDirection;
                    dir_2 = RandomDirection;
                    continue;
                }

                Portal portal_1 = Instantiate(portalPrefab, new Vector3(cell_1.x, 0, cell_1.y), Quaternion.Euler(0, (int)dir_1 * 90, 0), transform);
                Portal portal_2 = Instantiate(portalPrefab, new Vector3(cell_2.x, 0, cell_2.y), Quaternion.Euler(0, (int)dir_2 * 90, 0), transform);

                portalOcclusionVolume.portals.Add(portal_1);
                portalOcclusionVolume.portals.Add(portal_2);

                portal_1.targetPortal = portal_2;
                portal_2.targetPortal = portal_1;

                invalidCells.Add(cell_1);
                invalidCells.Add(cell_2);
                break;
            }
        }

        for (int i = 0; i < forceFieldCount; i++)
        {
            int tries = 0;
            Vector2Int cell = RandomCell;
            while (tries < 100)
            {
                if (!ValidForceFieldPlacement(cell))
                {
                    tries++;
                    cell = RandomCell;
                    continue;
                }

                Instantiate(forceFieldPrefab, new Vector3(cell.x, 0, cell.y), Quaternion.identity, transform);
                invalidCells.Add(cell);
                break;
            }
        }

    }

    [ContextMenu("Reset")]
    public void Reset()
    {
        DestroyChildren();
        leftEdges = null;
        bottomEdges = null;
        rooms = new RectInt[0];
        player.gameObject.SetActive(false);
        MovePlayer(Vector2Int.zero);
        portalOcclusionVolume.portals.Clear();
    }

    public Vector2Int RandomCell => new Vector2Int(Random.Range(0, width), Random.Range(0, height));

    public void Update()
    {
        WinCondition();
        GenerateRoute();
    }

    private Vector2Int cachedRouteStart;


    public void FirstRound()
    {
        level = 1;
        round = 1;
    }

    public void NextRound()
    {
        round++;
        if (round > 3)
        {
            round = 1;
            level++;
        }
    }

    public void GenerateRoute()
    {
        Vector2Int playerCell = GetCell(player.transform.position);
        if (cachedRouteStart == playerCell) 
            return;
        cachedRouteStart = playerCell;

        if (AStar(playerCell, endCell, out List<Vector2Int> path))
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            void AddQuad(Vector2Int p1, Vector2Int p2)
            {
                int index = vertices.Count;

                Vector3 normal = Vector3.up;
                Vector3 forward = (new Vector3(p2.x, 0, p2.y) - new Vector3(p1.x, 0, p1.y)).normalized;
                Vector3 right = Vector3.Cross(normal, forward) * routeGirth / 2f;

                vertices.Add(new Vector3(p1.x, 0, p1.y) - right);
                vertices.Add(new Vector3(p1.x, 0, p1.y) + right);
                vertices.Add(new Vector3(p2.x, 0, p2.y) + right);
                vertices.Add(new Vector3(p2.x, 0, p2.y) - right);

                float distance = Vector2.Distance(p1, p2);

                uvs.Add(new Vector2(0, 0));
                uvs.Add(new Vector2(0, distance));
                uvs.Add(new Vector2(1, distance));
                uvs.Add(new Vector2(1, 0));

                triangles.Add(index);
                triangles.Add(index + 2);
                triangles.Add(index + 1);

                triangles.Add(index + 2);
                triangles.Add(index);
                triangles.Add(index + 3);
            }

            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector2Int a = path[i + 0];
                Vector2Int b = path[i + 1];

                AddQuad(a, b);
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();

            if (route.mesh != null)
                Destroy(route.mesh);

            route.mesh = mesh;
        }
    }

    [ContextMenu("Auto Win Round")]
    private void WinRound()
    {
        GameManager uim = FindFirstObjectByType<GameManager>();
        if (uim.State != GameManager.GameState.InGame)
            return;
        if (level >= 2 && round >= 3)
            uim.ShowFinalScreen();
        else
            uim.ShowEndRoundScreen();
    }

    public void WinCondition()
    {
        if (player.gameObject.activeSelf == false)
            return;

        Vector2Int playerCell = GetCell(player.transform.position);
        if (playerCell != endCell)
            return;

        WinRound();
    }

    public void MovePlayer(Vector2Int cell)
    {
        player.transform.position = new Vector3(cell.x, 0.01f, cell.y);
    }

    public int GetEdgeType(Vector2Int a, Vector2Int b)
    {
        Vector2Int min = Vector2Int.Min(a, b);
        Vector2Int max = Vector2Int.Max(a, b);

        // if it's on the edge of the maze, it's a wall
        if (min.x == -1 && min.y >= 0 && min.y < height)
            return WALL;

        if (min.y == -1 && min.x >= 0 && min.x < width)
            return WALL;

        if (max.x == width && max.y >= 0 && max.y < height)
            return WALL;

        if (max.y == height && max.x >= 0 && max.x < width)
            return WALL;

        if (min.x < 0 || min.y < 0 || max.x >= width || max.y >= height)
            return OUT_OF_BOUNDS;

        if (min.x == max.x)
            return bottomEdges[min.y * width + min.x];

        else if (min.y == max.y)
            return leftEdges[min.y * (width - 1) + min.x];
        
        else
            throw new System.Exception("Edge must be horizontal or vertical");
            return -1; 

    }

    public bool IsPath(Vector2Int a, Vector2Int b)
    {
        Vector2Int min = Vector2Int.Min(a, b);
        Vector2Int max = Vector2Int.Max(a, b);

        if (min.x < 0 || min.y < 0 || max.x >= width || max.y >= height)
            return false;

        if (min.x == max.x)
        {
            int edge = bottomEdges[min.y * width + min.x];
            return edge == PATH || edge == DOOR;
        }

        else if (min.y == max.y)
        {
            int edge = leftEdges[min.y * (width - 1) + min.x];
            return edge == PATH || edge == DOOR;
        }
        
        else 
            throw new System.Exception("Edge must be horizontal or vertical");
    }

    public class PriorityQueue<T>
    {
        private List<(T, float)> elements = new List<(T, float)>();

        public int Count { get { return elements.Count; } }

        public void Enqueue(T item, float priority)
        {
            elements.Add((item, priority));
        }

        public T Dequeue()
        {
            int bestIndex = 0;
            for (int i = 0; i < elements.Count; i++)
            {
                if (elements[i].Item2 < elements[bestIndex].Item2)
                    bestIndex = i;
            }

            T bestItem = elements[bestIndex].Item1;
            elements.RemoveAt(bestIndex);
            return bestItem;
        }
    }

    public bool AStar(Vector2Int start, Vector2Int end, out List<Vector2Int> path)
    {
        path = new List<Vector2Int>();

        if (start == end)
        {
            path.Add(start);
            return true;
        }

        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        Dictionary<Vector2Int, float> costSoFar = new Dictionary<Vector2Int, float>();

        PriorityQueue<Vector2Int> frontier = new PriorityQueue<Vector2Int>();
        frontier.Enqueue(start, 0);

        cameFrom[start] = start;
        costSoFar[start] = 0;

        while (frontier.Count > 0)
        {
            Vector2Int current = frontier.Dequeue();

            if (current == end)
                break;

            foreach (Vector2Int next in GetNeighbours(current))
            {
                float newCost = costSoFar[current] + 1;
                if (!costSoFar.ContainsKey(next) || newCost < costSoFar[next])
                {
                    costSoFar[next] = newCost;
                    float priority = newCost + Vector2Int.Distance(next, end);
                    frontier.Enqueue(next, priority);
                    cameFrom[next] = current;
                }
            }
        }

        if (!cameFrom.ContainsKey(end))
            return false;

        Vector2Int step = end;
        while (step != start)
        {
            path.Add(step);
            step = cameFrom[step];
        }

        path.Add(start);
        path.Reverse();

        return true;
        
    }

    public Vector2Int[] GetCells()
    {
        Vector2Int[] cells = new Vector2Int[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                cells[y * width + x] = new Vector2Int(x, y);

        return cells;
    }

    public List<Vector2Int> GetNeighbours(Vector2Int cell)
    {
        // returns the accessible neighbours of a cell
        List<Vector2Int> neighbours = new List<Vector2Int>();

        Vector2Int forward = cell + Vector2Int.up;
        Vector2Int back = cell + Vector2Int.down;
        Vector2Int left = cell + Vector2Int.left;
        Vector2Int right = cell + Vector2Int.right;

        if (IsPath(cell, forward)) neighbours.Add(forward);
        if (IsPath(cell, back)) neighbours.Add(back);
        if (IsPath(cell, left)) neighbours.Add(left);
        if (IsPath(cell, right)) neighbours.Add(right);

        return neighbours;
    }

    public bool IsRoom(Vector2Int cell)
    {
        foreach (RectInt room in rooms)
        {
            if (cell.x >= room.x && cell.x < room.x + room.width && cell.y >= room.y && cell.y < room.y + room.height)
                return true;
        }

        return false;
    }

    public bool RoomOverlaps(RectInt room)
    {
        foreach (RectInt other in rooms)
            if (room.x < other.x + other.width && room.x + room.width > other.x && room.y < other.y + other.height && room.y + room.height > other.y)
                return true;

        return false;
    }

    [ContextMenu("Generate Random Rooms")]
    public void GenerateRandomRooms()
    {
        rooms = new RectInt[randomRoomCount];

        int tries = 0;
        for (int i = 0; i < randomRoomCount; i++)
        {
            int x = Random.Range(0, width - maxRoomSize);
            int z = Random.Range(0, height - maxRoomSize);
            int xSize = Random.Range(minRoomSize, maxRoomSize);
            int zSize = Random.Range(minRoomSize, maxRoomSize);

            RectInt room = new RectInt(x, z, xSize, zSize);
            if (RoomOverlaps(room))
            {
                if (tries > 100)
                {
                    Debug.LogWarning("Could not place room " + i);
                    tries = 0;
                    continue;
                }
                else
                {
                    i--;
                    tries++;
                    continue;
                }
            }           
            rooms[i] = room;
            tries = 0; 
        }
    }

    public void AddRoom(int x, int z, int xSize, int zSize)
    {
        // remove walls inside the room (
        for (int i = 0; i < xSize; i++)
        {
            for (int j = 0; j < zSize; j++)
            {
                if (i < xSize - 1)
                    leftEdges[(z + j) * (width - 1) + x + i] = PATH;

                if (j < zSize - 1)
                    bottomEdges[(z + j) * width + x + i] = PATH;
            }
        }

        // add walls around the room, unless doing so would cause cells to be disconnected
        
        for (int i = 0; i < xSize; i++)
        {
            if (z > 0)
            {
                bottomEdges[(z - 1) * width + x + i] = WALL;

                if (!AllConnected())
                    bottomEdges[(z - 1) * width + x + i] = DOOR;
            }

            if (z + zSize < height)
            {
                bottomEdges[(z + zSize - 1) * width + x + i] = WALL;

                if (!AllConnected())
                    bottomEdges[(z + zSize - 1) * width + x + i] = DOOR;
            }
        }

        for (int i = 0; i < zSize; i++)
        {
            if (x > 0)
            {
                leftEdges[(z + i) * (width - 1) + x - 1] = WALL;

                if (!AllConnected())
                    leftEdges[(z + i) * (width - 1) + x - 1] = DOOR;
            }

            if (x + xSize < width)
            {
                leftEdges[(z + i) * (width - 1) + x + xSize - 1] = WALL;
            
                if (!AllConnected())
                    leftEdges[(z + i) * (width - 1) + x + xSize - 1] = DOOR;
            }
        }
    }

    public void DestroyChildren()
    {
        var tempArray = new GameObject[transform.childCount];

        for(int i = 0; i < tempArray.Length; i++)
            tempArray[i] = transform.GetChild(i).gameObject;

        foreach(var child in tempArray)
            DestroyImmediate(child);

    }

    [ContextMenu("Instantiate Prefabs")]
    public void InstantiatePrefabs()
    {
        DestroyChildren();
        if (endCellPrefab != null)
            Instantiate(endCellPrefab, new Vector3(endCell.x, 0, endCell.y), Quaternion.identity, transform);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 position = new Vector3(x, 0, y);

                if (floorPrefab != null)
                {
                    Instantiate(floorPrefab, position, Quaternion.identity, transform);
                    // Ceiling
                    //Instantiate(floorPrefab, position + Vector3.up, Quaternion.Euler(180, 0, 0), transform);
                    
                }
                if (floorStonesPrefab != null && Random.value < 0.25f)
                {
                    Instantiate(floorStonesPrefab, position, Quaternion.Euler(0, Random.Range(0, 4) * 90, 0), transform);
                }
                if (x < width - 1)
                {
                    int edge = GetEdgeType(new Vector2Int(x, y), new Vector2Int(x + 1, y));
                    if (edge == WALL && wallPrefab != null)
                        Instantiate(wallPrefab, position + Vector3.right * 0.5f, Quaternion.Euler(0, 90, 0), transform);
                    else if (edge == DOOR && doorPrefab != null)
                        Instantiate(doorPrefab, position + Vector3.right * 0.5f, Quaternion.Euler(0, 90, 0), transform);
                }

                if (y < height - 1)
                {
                    int edge = GetEdgeType(new Vector2Int(x, y), new Vector2Int(x, y + 1));
                    if (edge == WALL && wallPrefab != null)
                        Instantiate(wallPrefab, position + Vector3.forward * 0.5f, Quaternion.identity, transform);
                    else if (edge == DOOR && doorPrefab != null)
                        Instantiate(doorPrefab, position + Vector3.forward * 0.5f, Quaternion.identity, transform);
                }
            }
        }

        // add walls around edges

        for (int x = 0; x < width; x++)
        {
            Instantiate(wallPrefab, new Vector3(x, 0, -0.5f), Quaternion.identity, transform);
            Instantiate(wallPrefab, new Vector3(x, 0, height - 0.5f), Quaternion.identity, transform);
        }

        for (int y = 0; y < height; y++)
        {
            Instantiate(wallPrefab, new Vector3(-0.5f, 0, y), Quaternion.Euler(0, 90, 0), transform);
            Instantiate(wallPrefab, new Vector3(width - 0.5f, 0, y), Quaternion.Euler(0, 90, 0), transform);
        }

        // add pillars at corners

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                int leftEdge = GetEdgeType(new Vector2Int(x - 1, z), new Vector2Int(x - 1, z - 1));
                int backwardEdge = GetEdgeType(new Vector2Int(x - 1, z - 1), new Vector2Int(x, z - 1));
                int rightEdge = GetEdgeType(new Vector2Int(x, z), new Vector2Int(x, z - 1));
                int forwardEdge = GetEdgeType(new Vector2Int(x - 1, z), new Vector2Int(x, z));

                bool leftWall = leftEdge == WALL || leftEdge == DOOR;
                bool backwardWall = backwardEdge == WALL || backwardEdge == DOOR;
                bool rightWall = rightEdge == WALL || rightEdge == DOOR;
                bool forwardWall = forwardEdge == WALL || forwardEdge == DOOR;


                int wallCount = 0;
                if (leftWall) wallCount++;
                if (backwardWall) wallCount++;
                if (rightWall) wallCount++;
                if (forwardWall) wallCount++;

                if (wallCount == 1)
                    Instantiate(pillarPrefab, new Vector3(x - 0.5f, 0, z - 0.5f), Quaternion.identity, transform);

                else if (wallCount == 2)
                {
                    if (forwardWall && leftWall)
                        Instantiate(pillarPrefab, new Vector3(x - 0.5f, 0, z - 0.5f), Quaternion.identity, transform);
      //                  Instantiate(pillarCornerPrefab, new Vector3(x - 0.5f, 0, z - 0.5f), Quaternion.identity, transform);

                    else if (leftWall && backwardWall)
                        Instantiate(pillarPrefab, new Vector3(x - 0.5f, 0, z - 0.5f), Quaternion.identity, transform);
    //                    Instantiate(pillarCornerPrefab, new Vector3(x - 0.5f, 0, z - 0.5f), Quaternion.Euler(0, 270, 0), transform);

                    else if (backwardWall && rightWall)
                        Instantiate(pillarPrefab, new Vector3(x - 0.5f, 0, z - 0.5f), Quaternion.identity, transform);
  //                      Instantiate(pillarCornerPrefab, new Vector3(x - 0.5f, 0, z - 0.5f), Quaternion.Euler(0, 180, 0), transform);

                    else if (rightWall && forwardWall)
                        Instantiate(pillarPrefab, new Vector3(x - 0.5f, 0, z - 0.5f), Quaternion.identity, transform);
//                        Instantiate(pillarCornerPrefab, new Vector3(x - 0.5f, 0, z - 0.5f), Quaternion.Euler(0, 90, 0), transform);
                }
            }
        }
    }

    /// <summary>
    /// Check if all cells are connected to one another
    /// </summary>
    /// <returns></returns>
    public bool AllConnected()
    {
        bool[] visited = new bool[width * height];
        int numVisited = 0;

        void Visit(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return;
            if (visited[y * width + x]) return;

            //Debug.Log("Visiting " + x + ", " + y + " total visited: " + numVisited + " out of " + (width * height));

            visited[y * width + x] = true;
            numVisited++;

            Vector2Int cell = new Vector2Int(x, y);
            Vector2Int left = new Vector2Int(x - 1, y);
            Vector2Int right = new Vector2Int(x + 1, y);
            Vector2Int forward = new Vector2Int(x, y + 1);
            Vector2Int back = new Vector2Int(x, y - 1);

            if (IsPath(cell, left)) Visit(x - 1, y);
            if (IsPath(cell, right)) Visit(x + 1, y);
            if (IsPath(cell, forward)) Visit(x, y + 1);
            if (IsPath(cell, back)) Visit(x, y - 1);
        }

        Visit(0, 0);

        return numVisited == width * height;

    }

    public void OnValidate()
    {
        GenerateMaze();
    }

    [ContextMenu("Generate")]
    public void GenerateMaze()
    {
        switch(algorithm)
        {
            case MazeAlgorithm.Kruskals:
                Kruskals(width, height, out leftEdges, out bottomEdges);
                break;
            case MazeAlgorithm.Prims:
                Prims(width, height, out leftEdges, out bottomEdges);
                break;
            case MazeAlgorithm.Wilsons:
                Wilsons(width, height, out leftEdges, out bottomEdges);
                break;
        }

        foreach (RectInt room in rooms)
            AddRoom(room.x, room.y, room.width, room.height);
    }

    public static void InitWalls(int width, int height, out int[] verticalWalls, out int[] horizontalWalls)
    {
        int numVerticalWalls = (width - 1) * height;
        int numHorizontalWalls = width * (height - 1);

        verticalWalls = new int[numVerticalWalls];
        horizontalWalls = new int[numHorizontalWalls];

        for (int i = 0; i < numVerticalWalls; i++) verticalWalls[i] = WALL;
        for (int i = 0; i < numHorizontalWalls; i++) horizontalWalls[i] = WALL;
    }

    public static void Prims(int width, int height, out int[] verticalWalls, out int[] horizontalWalls)
    {
            // Marks a cell as visited and adds its walls to the frontier
        void MarkCell(Vector2Int cell, int width, int height, bool[] visited, List<(Vector2Int, Vector2Int)> frontier)
        {
            int index = cell.y * width + cell.x;
            visited[index] = true;

            // Add neighboring walls to the frontier
            if (cell.x > 0) frontier.Add((cell, cell + Vector2Int.left));   // Left
            if (cell.x < width - 1) frontier.Add((cell, cell + Vector2Int.right));  // Right
            if (cell.y > 0) frontier.Add((cell, cell + Vector2Int.down));  // Down
            if (cell.y < height - 1) frontier.Add((cell, cell + Vector2Int.up));   // Up
        }

        // Removes the wall between two cells
        void RemoveWall(Vector2Int a, Vector2Int b, int width, int[] verticalWalls, int[] horizontalWalls)
        {
            if (a.x == b.x) // Vertical movement (removes horizontal wall)
            {
                int minY = Mathf.Min(a.y, b.y);
                horizontalWalls[minY * width + a.x] = PATH;
            }
            else if (a.y == b.y) // Horizontal movement (removes vertical wall)
            {
                int minX = Mathf.Min(a.x, b.x);
                verticalWalls[a.y * (width - 1) + minX] = PATH;
            }
        }

        InitWalls(width, height, out verticalWalls, out horizontalWalls);

        bool[] visited = new bool[width * height];

        List<(Vector2Int, Vector2Int)> frontier = new List<(Vector2Int, Vector2Int)>();

        // Start from a random cell
        Vector2Int start = new Vector2Int(Random.Range(0, width), Random.Range(0, height));
        MarkCell(start, width, height, visited, frontier);

        // Process walls
        while (frontier.Count > 0)
        {
            int randIndex = Random.Range(0, frontier.Count);
            var (a, b) = frontier[randIndex];
            frontier.RemoveAt(randIndex);

            int aIndex = a.y * width + a.x;
            int bIndex = b.y * width + b.x;

            if (!visited[bIndex])
            {
                visited[bIndex] = true;
                RemoveWall(a, b, width, verticalWalls, horizontalWalls);
                MarkCell(b, width, height, visited, frontier);
            }
        }
    }

    public static void Kruskals(int width, int height, out int[] verticalWalls, out int[] horizontalWalls)
    {
        // Disjoint Set Find with Path Compression
        int Find(int[] parent, int i)
        {
            if (parent[i] != i) parent[i] = Find(parent, parent[i]);
            return parent[i];
        }

        // Disjoint Set Union
        void Union(int[] parent, int a, int b)
        {
            int rootA = Find(parent, a);
            int rootB = Find(parent, b);
            if (rootA != rootB) parent[rootB] = rootA;
        }

        // Fisher-Yates shuffle
        void Shuffle(List<(Vector2Int, bool)> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        InitWalls(width, height, out verticalWalls, out horizontalWalls);

        // Disjoint Set (Union-Find) to track connected nodes
        int[] parent = new int[width * height];
        for (int i = 0; i < parent.Length; i++) parent[i] = i;

        // Generate all walls and shuffle them
        List<(Vector2Int, bool)> walls = new List<(Vector2Int, bool)>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (x < width - 1) walls.Add((new Vector2Int(x, y), true));  // Vertical wall
                if (y < height - 1) walls.Add((new Vector2Int(x, y), false)); // Horizontal wall
            }
        }

        Shuffle(walls); // Randomise wall processing order

        // Process walls
        foreach (var (cell, isVertical) in walls)
        {
            int x = cell.x, y = cell.y;
            int aIndex = y * width + x;
            int bIndex = isVertical ? aIndex + 1 : aIndex + width;

            if (Find(parent, aIndex) != Find(parent, bIndex))
            {
                Union(parent, aIndex, bIndex);

                if (isVertical)
                    verticalWalls[y * (width - 1) + x] = PATH;  // Remove vertical wall
                else
                    horizontalWalls[y * width + x] = PATH;  // Remove horizontal wall
            }
        }
    }


    public static void Wilsons(int width, int height, out int[] verticalWalls, out int[] horizontalWalls)
    {
        // Gets a random neighboring cell
        Vector2Int GetRandomNeighbor(Vector2Int cell, int width, int height)
        {
            List<Vector2Int> neighbors = new List<Vector2Int>();

            if (cell.x > 0) neighbors.Add(cell + Vector2Int.left);
            if (cell.x < width - 1) neighbors.Add(cell + Vector2Int.right);
            if (cell.y > 0) neighbors.Add(cell + Vector2Int.down);
            if (cell.y < height - 1) neighbors.Add(cell + Vector2Int.up);

            return neighbors[Random.Range(0, neighbors.Count)];
        }

        // Removes the wall between two cells
        void RemoveWall(Vector2Int a, Vector2Int b, int width, int[] verticalWalls, int[] horizontalWalls)
        {
            if (a.x == b.x) // Vertical movement (removes horizontal wall)
            {
                int minY = Mathf.Min(a.y, b.y);
                horizontalWalls[minY * width + a.x] = PATH;
            }
            else if (a.y == b.y) // Horizontal movement (removes vertical wall)
            {
                int minX = Mathf.Min(a.x, b.x);
                verticalWalls[a.y * (width - 1) + minX] = PATH;
            }
        }

        InitWalls(width, height, out verticalWalls, out horizontalWalls);

        bool[] inMaze = new bool[width * height];
        List<Vector2Int> unvisited = new List<Vector2Int>();

        // Populate the list of unvisited cells
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                unvisited.Add(new Vector2Int(x, y));
            }
        }

        // Start with one random cell in the maze
        Vector2Int start = unvisited[Random.Range(0, unvisited.Count)];
        inMaze[start.y * width + start.x] = true;
        unvisited.Remove(start);

        // Process each unvisited cell
        while (unvisited.Count > 0)
        {
            // Pick a random starting cell from unvisited
            Vector2Int current = unvisited[Random.Range(0, unvisited.Count)];
            List<Vector2Int> path = new List<Vector2Int> { current };
            Dictionary<Vector2Int, Vector2Int> backtrack = new Dictionary<Vector2Int, Vector2Int>();

            // Perform a random walk until reaching the maze
            while (!inMaze[current.y * width + current.x])
            {
                Vector2Int next = GetRandomNeighbor(current, width, height);

                // If we revisit a node, erase the loop
                if (backtrack.ContainsKey(next))
                {
                    while (path.Count > 0 && path[path.Count - 1] != next)
                    {
                        path.RemoveAt(path.Count - 1);
                    }
                }
                else
                {
                    backtrack[next] = current;
                    path.Add(next);
                }

                current = next;
            }

            // Add the valid path to the maze
            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector2Int a = path[i];
                Vector2Int b = path[i + 1];

                inMaze[a.y * width + a.x] = true;
                unvisited.Remove(a);
                RemoveWall(a, b, width, verticalWalls, horizontalWalls);
            }
        }
    }

    public Vector2Int GetCell(Vector3 worldPosition)
    {
        return new Vector2Int(Mathf.RoundToInt(worldPosition.x), Mathf.RoundToInt(worldPosition.z));
    }

    public void OnDrawGizmos()
    {
        Vector2Int playerCell = GetCell(player.transform.position);
        if (AStar(playerCell, endCell, out List<Vector2Int> path))
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector3 a = new Vector3(path[i].x, 0, path[i].y);
                Vector3 b = new Vector3(path[i + 1].x, 0, path[i + 1].y);
                Gizmos.DrawLine(a, b);
            }
        }

        if (true) return;

        Vector3 c00 = new Vector3(-0.5f, 0, -0.5f);
        Vector3 c10 = new Vector3(width - 0.5f, 0, -0.5f);
        Vector3 c01 = new Vector3(-0.5f, 0, height - 0.5f);
        Vector3 c11 = new Vector3(width - 0.5f, 0, height - 0.5f);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(c00, c10);
        Gizmos.DrawLine(c10, c11);
        Gizmos.DrawLine(c11, c01);
        Gizmos.DrawLine(c01, c00);

        foreach (RectInt room in rooms)
        {
            Vector3 center = new Vector3(room.x + (room.width - 1) * 0.5f, 0, room.y + (room.height - 1) * 0.5f);
            Vector3 size = new Vector3(room.width, 0, room.height);
            Gizmos.color = Color.green;
            Gizmos.DrawCube(center, size);
        }

        Color transparent = new Color(1, 1, 1, 0.25f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 cell = new Vector3(x, 0, y);
                Gizmos.color = transparent;
                Gizmos.DrawSphere(cell, 0.1f);

                if (x < width - 1) 
                {
                    int edge = GetEdgeType(new Vector2Int(x, y), new Vector2Int(x + 1, y));
                    if (edge == PATH)
                    {
                        Gizmos.color = transparent;
                        Gizmos.DrawLine(cell, cell + Vector3.right);
                    }
                    else if (edge == WALL)
                    {
                        Gizmos.color = Color.blue;
                        Gizmos.DrawLine(cell + (Vector3.right + Vector3.back) * 0.5f, cell + (Vector3.right + Vector3.forward) * 0.5f);
                    }
                    else if (edge == DOOR)
                    {
                        Gizmos.color = transparent;
                        Gizmos.DrawLine(cell, cell + Vector3.right);
                        Gizmos.color = Color.red;
                        Gizmos.DrawLine(cell + (Vector3.right + Vector3.back) * 0.5f, cell + (Vector3.right + Vector3.forward) * 0.5f);
                    }
                }

                if (y < height - 1)
                {
                    int edge = GetEdgeType(new Vector2Int(x, y), new Vector2Int(x, y + 1));
                    if (edge == PATH)
                    {
                        Gizmos.color = transparent;
                        Gizmos.DrawLine(cell, cell + Vector3.forward);
                    }
                    else if (edge == DOOR)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawLine(cell + (Vector3.left + Vector3.forward) * 0.5f, cell + (Vector3.right + Vector3.forward) * 0.5f);
                    }
                    else if (edge == WALL)
                    {
                        Gizmos.color = Color.blue;
                        Gizmos.DrawLine(cell + (Vector3.left + Vector3.forward) * 0.5f, cell + (Vector3.right + Vector3.forward) * 0.5f);
                    }
                }
            }
        }
    }
}
