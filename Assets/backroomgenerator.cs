using System.Collections.Generic;
using UnityEngine;

public class BackroomsWorldGenerator : MonoBehaviour
{
    // =========================================================
    // CONFIGURACIÓN DEL MUNDO
    // =========================================================

    [Header("Tamaño del mundo")]
    [Min(2)] public int roomsX = 18;
    [Min(2)] public int roomsZ = 18;

    [Tooltip("Tamaño de cada espacio caminable.")]
    public float tileSize = 4f;

    [Header("Altura y grosor")]
    public float wallHeight = 3f;
    public float floorThickness = 0.15f;
    public float ceilingThickness = 0.15f;

    // =========================================================
    // ESTILO DEL LABERINTO
    // =========================================================

    [Header("Estilo Backrooms")]

    [Range(0f, 0.5f)]
    [Tooltip("Añade conexiones extra para que no sea un laberinto tradicional perfecto.")]
    public float extraConnectionChance = 0.12f;

    [Tooltip("Cantidad de zonas abiertas que se crearán.")]
    public int roomCarves = 10;

    [Tooltip("Tamaño máximo de las zonas abiertas.")]
    public int maxRoomRadius = 2;

    [Tooltip("Elimina paredes aisladas que parecen postes.")]
    public int cleanupIterations = 2;

    // =========================================================
    // MATERIALES
    // =========================================================

    [Header("Materiales")]
    public Material wallMaterial;
    public Material floorMaterial;
    public Material ceilingMaterial;

    // =========================================================
    // LUCES
    // =========================================================

    [Header("Luces")]
    public bool generateLights = true;

    [Min(1)]
    public int lightSpacingTiles = 5;

    public float lightIntensity = 0.9f;
    public float lightRange = 9f;

    [Tooltip("Opcional. Si está vacío, se crearán Point Lights automáticamente.")]
    public GameObject lightPrefab;

    // =========================================================
    // JUGADOR
    // =========================================================

    [Header("Jugador")]
    public Transform player;
    public float playerSpawnHeight = 1.1f;

    // =========================================================
    // OBJETIVOS
    // =========================================================

    [Header("Palancas y salida")]

    [Tooltip("Se crea automáticamente si no asignas uno.")]
    public BackroomsObjectiveManager objectiveManager;

    [Min(1)]
    public int requiredLevers = 3;

    [Tooltip("Opcional. Si está vacío, se construye una palanca básica.")]
    public GameObject leverPrefab;

    [Tooltip("Opcional. Si está vacío, se construye una salida básica.")]
    public GameObject exitPrefab;

    public Material leverMaterial;
    public Material exitMaterial;

    [Tooltip("Distancia mínima desde el inicio, medida en casillas.")]
    public int objectiveMinPathDistance = 8;

    [Tooltip("Distancia mínima en metros entre las palancas.")]
    public float objectiveMinWorldDistanceBetween = 12f;

    // =========================================================
    // GENERACIÓN
    // =========================================================

    [Header("Generación")]
    public bool generateOnStart = true;
    public bool randomSeed = true;
    public int seed = 12345;

    private bool[,] openTiles;

    private int gridWidth;
    private int gridDepth;

    private readonly Vector2Int[] directions =
    {
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0)
    };

    // =========================================================
    // INICIO
    // =========================================================

    private void Start()
    {
        if (generateOnStart)
        {
            Generate();
        }
    }

    [ContextMenu("Generate Backrooms")]
    public void Generate()
    {
        InitializeRandomSeed();

        ClearOldWorld();

        BuildMazeLayout();
        AddExtraConnections();
        CarveOpenAreas();
        CleanupLonelyWalls();
        CloseOuterBorder();

        BuildGeometry();
        BuildLights();

        PlacePlayer();
        SpawnObjectives();

        Debug.Log(
            $"Backrooms generados. Seed: {seed}. " +
            $"Palancas requeridas: {requiredLevers}."
        );
    }

    // =========================================================
    // SEMILLA
    // =========================================================

    private void InitializeRandomSeed()
    {
        if (randomSeed)
        {
            seed = System.Environment.TickCount;
        }

        Random.InitState(seed);
    }

    // =========================================================
    // GENERACIÓN DEL LABERINTO
    // =========================================================

    private void BuildMazeLayout()
    {
        gridWidth = roomsX * 2 + 1;
        gridDepth = roomsZ * 2 + 1;

        openTiles = new bool[gridWidth, gridDepth];

        bool[,] visited = new bool[roomsX, roomsZ];

        Stack<Vector2Int> stack = new Stack<Vector2Int>();

        Vector2Int currentRoom = new Vector2Int(0, 0);

        visited[currentRoom.x, currentRoom.y] = true;

        OpenRoomCell(currentRoom);

        stack.Push(currentRoom);

        while (stack.Count > 0)
        {
            currentRoom = stack.Peek();

            List<Vector2Int> unvisitedNeighbors =
                GetUnvisitedNeighbors(currentRoom, visited);

            if (unvisitedNeighbors.Count == 0)
            {
                stack.Pop();
                continue;
            }

            Vector2Int selectedRoom =
                unvisitedNeighbors[Random.Range(0, unvisitedNeighbors.Count)];

            OpenPassageBetweenRooms(currentRoom, selectedRoom);
            OpenRoomCell(selectedRoom);

            visited[selectedRoom.x, selectedRoom.y] = true;

            stack.Push(selectedRoom);
        }

        CloseOuterBorder();
    }

    private void OpenRoomCell(Vector2Int room)
    {
        int gridX = room.x * 2 + 1;
        int gridZ = room.y * 2 + 1;

        openTiles[gridX, gridZ] = true;
    }

    private void OpenPassageBetweenRooms(
        Vector2Int currentRoom,
        Vector2Int nextRoom
    )
    {
        int currentGridX = currentRoom.x * 2 + 1;
        int currentGridZ = currentRoom.y * 2 + 1;

        int directionX = nextRoom.x - currentRoom.x;
        int directionZ = nextRoom.y - currentRoom.y;

        int passageX = currentGridX + directionX;
        int passageZ = currentGridZ + directionZ;

        openTiles[passageX, passageZ] = true;
    }

    private List<Vector2Int> GetUnvisitedNeighbors(
        Vector2Int current,
        bool[,] visited
    )
    {
        List<Vector2Int> result = new List<Vector2Int>();

        foreach (Vector2Int direction in directions)
        {
            Vector2Int neighbor = current + direction;

            bool insideGrid =
                neighbor.x >= 0 &&
                neighbor.x < roomsX &&
                neighbor.y >= 0 &&
                neighbor.y < roomsZ;

            if (!insideGrid)
                continue;

            if (!visited[neighbor.x, neighbor.y])
            {
                result.Add(neighbor);
            }
        }

        return result;
    }

    // =========================================================
    // CONEXIONES EXTRA
    // =========================================================

    private void AddExtraConnections()
    {
        for (int x = 1; x < gridWidth - 1; x++)
        {
            for (int z = 1; z < gridDepth - 1; z++)
            {
                if (openTiles[x, z])
                    continue;

                bool horizontalConnection =
                    openTiles[x - 1, z] &&
                    openTiles[x + 1, z];

                bool verticalConnection =
                    openTiles[x, z - 1] &&
                    openTiles[x, z + 1];

                bool canOpen =
                    horizontalConnection ||
                    verticalConnection;

                if (canOpen && Random.value < extraConnectionChance)
                {
                    openTiles[x, z] = true;
                }
            }
        }

        CloseOuterBorder();
    }

    // =========================================================
    // ESPACIOS ABIERTOS
    // =========================================================

    private void CarveOpenAreas()
    {
        int safeRadius = Mathf.Max(1, maxRoomRadius);

        for (int i = 0; i < roomCarves; i++)
        {
            int centerX = Random.Range(2, gridWidth - 2);
            int centerZ = Random.Range(2, gridDepth - 2);

            int radiusX = Random.Range(1, safeRadius + 1);
            int radiusZ = Random.Range(1, safeRadius + 1);

            for (
                int x = centerX - radiusX;
                x <= centerX + radiusX;
                x++
            )
            {
                for (
                    int z = centerZ - radiusZ;
                    z <= centerZ + radiusZ;
                    z++
                )
                {
                    bool insideBorder =
                        x > 0 &&
                        x < gridWidth - 1 &&
                        z > 0 &&
                        z < gridDepth - 1;

                    if (insideBorder)
                    {
                        openTiles[x, z] = true;
                    }
                }
            }
        }

        CloseOuterBorder();
    }

    // =========================================================
    // LIMPIEZA DE PAREDES AISLADAS
    // =========================================================

    private void CleanupLonelyWalls()
    {
        for (
            int iteration = 0;
            iteration < cleanupIterations;
            iteration++
        )
        {
            List<Vector2Int> wallsToRemove =
                new List<Vector2Int>();

            for (int x = 1; x < gridWidth - 1; x++)
            {
                for (int z = 1; z < gridDepth - 1; z++)
                {
                    if (openTiles[x, z])
                        continue;

                    int openNeighbors = CountOpenNeighbors(x, z);

                    // Si una pared está rodeada por espacios abiertos,
                    // parecería un poste aislado. Por eso se elimina.
                    if (openNeighbors >= 3)
                    {
                        wallsToRemove.Add(
                            new Vector2Int(x, z)
                        );
                    }
                }
            }

            foreach (Vector2Int wall in wallsToRemove)
            {
                openTiles[wall.x, wall.y] = true;
            }
        }

        CloseOuterBorder();
    }

    private int CountOpenNeighbors(int x, int z)
    {
        int count = 0;

        if (openTiles[x + 1, z]) count++;
        if (openTiles[x - 1, z]) count++;
        if (openTiles[x, z + 1]) count++;
        if (openTiles[x, z - 1]) count++;

        return count;
    }

    private void CloseOuterBorder()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            openTiles[x, 0] = false;
            openTiles[x, gridDepth - 1] = false;
        }

        for (int z = 0; z < gridDepth; z++)
        {
            openTiles[0, z] = false;
            openTiles[gridWidth - 1, z] = false;
        }
    }

    // =========================================================
    // CONSTRUCCIÓN DEL MUNDO
    // =========================================================

    private void BuildGeometry()
    {
        CreateFloor();
        CreateCeiling();
        CreateMergedWalls();
    }

    private void CreateFloor()
    {
        CreateBlock(
            "Floor",
            new Vector3(
                0f,
                -floorThickness * 0.5f,
                0f
            ),
            new Vector3(
                gridWidth * tileSize,
                floorThickness,
                gridDepth * tileSize
            ),
            floorMaterial
        );
    }

    private void CreateCeiling()
    {
        CreateBlock(
            "Ceiling",
            new Vector3(
                0f,
                wallHeight + ceilingThickness * 0.5f,
                0f
            ),
            new Vector3(
                gridWidth * tileSize,
                ceilingThickness,
                gridDepth * tileSize
            ),
            ceilingMaterial
        );
    }

    private void CreateMergedWalls()
    {
        bool[,] usedWalls =
            new bool[gridWidth, gridDepth];

        for (int z = 0; z < gridDepth; z++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                if (openTiles[x, z] || usedWalls[x, z])
                    continue;

                int rectangleWidth =
                    FindAvailableWallWidth(x, z, usedWalls);

                int rectangleDepth =
                    FindAvailableWallDepth(
                        x,
                        z,
                        rectangleWidth,
                        usedWalls
                    );

                MarkWallRectangleAsUsed(
                    x,
                    z,
                    rectangleWidth,
                    rectangleDepth,
                    usedWalls
                );

                CreateWallRectangle(
                    x,
                    z,
                    rectangleWidth,
                    rectangleDepth
                );
            }
        }
    }

    private int FindAvailableWallWidth(
        int startX,
        int startZ,
        bool[,] usedWalls
    )
    {
        int width = 1;

        while (startX + width < gridWidth)
        {
            int checkX = startX + width;

            if (openTiles[checkX, startZ])
                break;

            if (usedWalls[checkX, startZ])
                break;

            width++;
        }

        return width;
    }

    private int FindAvailableWallDepth(
        int startX,
        int startZ,
        int rectangleWidth,
        bool[,] usedWalls
    )
    {
        int depth = 1;
        bool canGrow = true;

        while (startZ + depth < gridDepth && canGrow)
        {
            int checkZ = startZ + depth;

            for (
                int x = startX;
                x < startX + rectangleWidth;
                x++
            )
            {
                if (
                    openTiles[x, checkZ] ||
                    usedWalls[x, checkZ]
                )
                {
                    canGrow = false;
                    break;
                }
            }

            if (canGrow)
            {
                depth++;
            }
        }

        return depth;
    }

    private void MarkWallRectangleAsUsed(
        int startX,
        int startZ,
        int width,
        int depth,
        bool[,] usedWalls
    )
    {
        for (int x = startX; x < startX + width; x++)
        {
            for (int z = startZ; z < startZ + depth; z++)
            {
                usedWalls[x, z] = true;
            }
        }
    }

    private void CreateWallRectangle(
        int startX,
        int startZ,
        int width,
        int depth
    )
    {
        Vector3 center = TileCenter(startX, startZ);

        center.x += (width - 1) * tileSize * 0.5f;
        center.z += (depth - 1) * tileSize * 0.5f;
        center.y = wallHeight * 0.5f;

        Vector3 size = new Vector3(
            width * tileSize,
            wallHeight,
            depth * tileSize
        );

        CreateBlock(
            "Wall_Block",
            center,
            size,
            wallMaterial
        );
    }

    // =========================================================
    // LUCES
    // =========================================================

    private void BuildLights()
    {
        if (!generateLights)
            return;

        int spacing = Mathf.Max(1, lightSpacingTiles);

        for (int x = 1; x < gridWidth - 1; x += spacing)
        {
            for (int z = 1; z < gridDepth - 1; z += spacing)
            {
                if (!openTiles[x, z])
                    continue;

                Vector3 lightPosition = TileCenter(x, z);
                lightPosition.y = wallHeight - 0.25f;

                CreateLight(lightPosition);
            }
        }
    }

    private void CreateLight(Vector3 localPosition)
    {
        if (lightPrefab != null)
        {
            GameObject createdLight =
                Instantiate(lightPrefab, transform);

            createdLight.name = "Backrooms_Light";
            createdLight.transform.localPosition = localPosition;

            return;
        }

        GameObject lightObject =
            new GameObject("Backrooms_Light");

        lightObject.transform.SetParent(transform, false);
        lightObject.transform.localPosition = localPosition;

        Light pointLight = lightObject.AddComponent<Light>();

        pointLight.type = LightType.Point;
        pointLight.intensity = lightIntensity;
        pointLight.range = lightRange;
        pointLight.color = new Color(
            1f,
            0.86f,
            0.55f
        );

        GameObject fluorescent = CreateBlock(
            "Fluorescent_Mesh",
            localPosition + new Vector3(0f, 0.05f, 0f),
            new Vector3(
                tileSize * 0.6f,
                0.04f,
                tileSize * 0.15f
            ),
            ceilingMaterial
        );

        RemoveCollider(fluorescent);
    }

    // =========================================================
    // JUGADOR
    // =========================================================

    private void PlacePlayer()
    {
        if (player == null)
        {
            Debug.LogWarning(
                "No asignaste el Player en BackroomsWorldGenerator."
            );

            return;
        }

        Vector2Int startTile = GetStartTile();

        Vector3 spawnPosition =
            TileCenter(startTile.x, startTile.y);

        spawnPosition.y = playerSpawnHeight;

        CharacterController characterController =
            player.GetComponent<CharacterController>();

        if (characterController != null)
        {
            characterController.enabled = false;
        }

        player.position =
            transform.TransformPoint(spawnPosition);

        player.rotation =
            transform.rotation;

        if (characterController != null)
        {
            characterController.enabled = true;
        }
    }

    private Vector2Int GetStartTile()
    {
        Vector2Int preferredStart =
            new Vector2Int(1, 1);

        if (
            IsInsideGrid(preferredStart.x, preferredStart.y) &&
            openTiles[preferredStart.x, preferredStart.y]
        )
        {
            return preferredStart;
        }

        for (int x = 1; x < gridWidth - 1; x++)
        {
            for (int z = 1; z < gridDepth - 1; z++)
            {
                if (openTiles[x, z])
                {
                    return new Vector2Int(x, z);
                }
            }
        }

        return preferredStart;
    }

    // =========================================================
    // OBJETIVOS
    // =========================================================

    private void SpawnObjectives()
    {
        EnsureObjectiveManager();

        Vector2Int startTile = GetStartTile();

        int[,] distances =
            CalculateOpenTileDistances(startTile);

        Vector2Int exitTile =
            FindFarthestReachableTile(distances);

        List<Vector2Int> leverTiles =
            ChooseLeverTiles(
                distances,
                startTile,
                exitTile
            );

        if (leverTiles.Count == 0)
        {
            Debug.LogError(
                "No se encontraron posiciones para las palancas."
            );

            return;
        }

        GameObject exitObject =
            CreateExitObject(exitTile);

        objectiveManager.Setup(
            leverTiles.Count,
            exitObject
        );

        for (int i = 0; i < leverTiles.Count; i++)
        {
            CreateLeverObject(
                leverTiles[i],
                i + 1
            );
        }

        if (leverTiles.Count < requiredLevers)
        {
            Debug.LogWarning(
                $"Solo se pudieron colocar {leverTiles.Count} " +
                $"palancas de las {requiredLevers} solicitadas. " +
                "Aumenta Rooms X, Rooms Z o reduce las distancias mínimas."
            );
        }
    }

    private void EnsureObjectiveManager()
    {
        if (objectiveManager != null)
            return;

        objectiveManager =
            FindFirstObjectByType<BackroomsObjectiveManager>();

        if (objectiveManager != null)
            return;

        GameObject managerObject =
            new GameObject("BackroomsObjectiveManager");

        objectiveManager =
            managerObject.AddComponent<BackroomsObjectiveManager>();
    }

    // =========================================================
    // BFS Y DISTANCIAS
    // =========================================================

    private int[,] CalculateOpenTileDistances(
        Vector2Int start
    )
    {
        int[,] distances =
            new int[gridWidth, gridDepth];

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridDepth; z++)
            {
                distances[x, z] = -1;
            }
        }

        if (
            !IsInsideGrid(start.x, start.y) ||
            !openTiles[start.x, start.y]
        )
        {
            return distances;
        }

        Queue<Vector2Int> queue =
            new Queue<Vector2Int>();

        queue.Enqueue(start);
        distances[start.x, start.y] = 0;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            foreach (Vector2Int direction in directions)
            {
                Vector2Int next = current + direction;

                if (!IsInsideGrid(next.x, next.y))
                    continue;

                if (!openTiles[next.x, next.y])
                    continue;

                if (distances[next.x, next.y] != -1)
                    continue;

                distances[next.x, next.y] =
                    distances[current.x, current.y] + 1;

                queue.Enqueue(next);
            }
        }

        return distances;
    }

    private Vector2Int FindFarthestReachableTile(
        int[,] distances
    )
    {
        Vector2Int farthestTile = GetStartTile();
        int farthestDistance = -1;

        // Primero intenta encontrar una posición junto a una pared.
        for (int x = 1; x < gridWidth - 1; x++)
        {
            for (int z = 1; z < gridDepth - 1; z++)
            {
                if (!openTiles[x, z])
                    continue;

                if (distances[x, z] < 0)
                    continue;

                Vector2Int tile = new Vector2Int(x, z);

                if (!HasAdjacentWall(tile))
                    continue;

                if (distances[x, z] > farthestDistance)
                {
                    farthestDistance = distances[x, z];
                    farthestTile = tile;
                }
            }
        }

        // Respaldo por si no existe ninguna pared cercana.
        if (farthestDistance < 0)
        {
            for (int x = 1; x < gridWidth - 1; x++)
            {
                for (int z = 1; z < gridDepth - 1; z++)
                {
                    if (
                        openTiles[x, z] &&
                        distances[x, z] > farthestDistance
                    )
                    {
                        farthestDistance = distances[x, z];
                        farthestTile = new Vector2Int(x, z);
                    }
                }
            }
        }

        return farthestTile;
    }

    // =========================================================
    // ELECCIÓN DE PALANCAS
    // =========================================================

    private List<Vector2Int> ChooseLeverTiles(
        int[,] distances,
        Vector2Int startTile,
        Vector2Int exitTile
    )
    {
        List<Vector2Int> candidates =
            new List<Vector2Int>();

        for (int x = 1; x < gridWidth - 1; x++)
        {
            for (int z = 1; z < gridDepth - 1; z++)
            {
                Vector2Int tile = new Vector2Int(x, z);

                if (!openTiles[x, z])
                    continue;

                if (distances[x, z] < objectiveMinPathDistance)
                    continue;

                if (tile == startTile || tile == exitTile)
                    continue;

                if (!HasAdjacentWall(tile))
                    continue;

                float distanceFromExit =
                    Vector2Int.Distance(tile, exitTile);

                if (distanceFromExit < 3f)
                    continue;

                candidates.Add(tile);
            }
        }

        ShuffleList(candidates);

        List<Vector2Int> selected =
            new List<Vector2Int>();

        // Primer recorrido: respeta la separación solicitada.
        foreach (Vector2Int candidate in candidates)
        {
            if (selected.Count >= requiredLevers)
                break;

            if (
                IsFarEnoughFromSelected(
                    candidate,
                    selected,
                    objectiveMinWorldDistanceBetween
                )
            )
            {
                selected.Add(candidate);
            }
        }

        // Segundo recorrido: relaja la distancia para no romper la partida.
        if (selected.Count < requiredLevers)
        {
            foreach (Vector2Int candidate in candidates)
            {
                if (selected.Count >= requiredLevers)
                    break;

                if (!selected.Contains(candidate))
                {
                    selected.Add(candidate);
                }
            }
        }

        // Respaldo adicional para laberintos muy pequeños.
        if (selected.Count < requiredLevers)
        {
            AddEmergencyLeverTiles(
                selected,
                distances,
                startTile,
                exitTile
            );
        }

        return selected;
    }

    private bool IsFarEnoughFromSelected(
        Vector2Int candidate,
        List<Vector2Int> selected,
        float minimumDistance
    )
    {
        Vector3 candidatePosition =
            TileCenter(candidate.x, candidate.y);

        foreach (Vector2Int existing in selected)
        {
            Vector3 existingPosition =
                TileCenter(existing.x, existing.y);

            float distance = Vector3.Distance(
                candidatePosition,
                existingPosition
            );

            if (distance < minimumDistance)
            {
                return false;
            }
        }

        return true;
    }

    private void AddEmergencyLeverTiles(
        List<Vector2Int> selected,
        int[,] distances,
        Vector2Int startTile,
        Vector2Int exitTile
    )
    {
        for (int x = 1; x < gridWidth - 1; x++)
        {
            for (int z = 1; z < gridDepth - 1; z++)
            {
                if (selected.Count >= requiredLevers)
                    return;

                Vector2Int tile = new Vector2Int(x, z);

                if (!openTiles[x, z])
                    continue;

                if (distances[x, z] < 0)
                    continue;

                if (tile == startTile || tile == exitTile)
                    continue;

                if (!HasAdjacentWall(tile))
                    continue;

                if (!selected.Contains(tile))
                {
                    selected.Add(tile);
                }
            }
        }
    }

    private void ShuffleList(List<Vector2Int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            Vector2Int temporary = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temporary;
        }
    }

    // =========================================================
    // CREACIÓN DE PALANCAS
    // =========================================================

    private void CreateLeverObject(
        Vector2Int tile,
        int leverNumber
    )
    {
        GetWallPlacement(
            tile,
            out Vector3 localPosition,
            out Quaternion localRotation
        );

        localPosition.y = 0f;

        GameObject leverObject;

        if (leverPrefab != null)
        {
            leverObject =
                Instantiate(leverPrefab, transform);

            leverObject.name =
                $"Procedural_Lever_{leverNumber}";

            leverObject.transform.localPosition =
                localPosition;

            leverObject.transform.localRotation =
                localRotation;
        }
        else
        {
            leverObject = CreateDefaultLever(
                localPosition,
                localRotation,
                leverNumber
            );
        }

        BackroomsLever leverScript =
            leverObject.GetComponent<BackroomsLever>();

        if (leverScript == null)
        {
            leverScript =
                leverObject.AddComponent<BackroomsLever>();
        }

        leverScript.manager = objectiveManager;

        SphereCollider trigger =
            leverObject.GetComponent<SphereCollider>();

        if (trigger == null)
        {
            trigger =
                leverObject.AddComponent<SphereCollider>();
        }

        trigger.isTrigger = true;
        trigger.radius = 1.25f;
        trigger.center = new Vector3(
            0f,
            1f,
            0.35f
        );
    }

    private GameObject CreateDefaultLever(
        Vector3 localPosition,
        Quaternion localRotation,
        int leverNumber
    )
    {
        GameObject root =
            new GameObject(
                $"Procedural_Lever_{leverNumber}"
            );

        root.transform.SetParent(transform, false);
        root.transform.localPosition = localPosition;
        root.transform.localRotation = localRotation;

        GameObject plate = CreatePrimitiveChild(
            "Lever_Plate",
            PrimitiveType.Cube,
            root.transform,
            new Vector3(0f, 1.1f, 0f),
            new Vector3(0.55f, 1.1f, 0.12f),
            leverMaterial
        );

        GameObject baseObject = CreatePrimitiveChild(
            "Lever_Base",
            PrimitiveType.Cube,
            root.transform,
            new Vector3(0f, 1.1f, 0.18f),
            new Vector3(0.32f, 0.32f, 0.32f),
            leverMaterial
        );

        GameObject handle = CreatePrimitiveChild(
            "Lever_Handle",
            PrimitiveType.Cube,
            root.transform,
            new Vector3(0f, 1.25f, 0.42f),
            new Vector3(0.12f, 0.75f, 0.12f),
            leverMaterial
        );

        handle.transform.localRotation =
            Quaternion.Euler(35f, 0f, 0f);

        DisableCollider(plate);
        DisableCollider(baseObject);
        DisableCollider(handle);

        BackroomsLever leverScript =
            root.AddComponent<BackroomsLever>();

        leverScript.manager = objectiveManager;
        leverScript.handle = handle.transform;

        return root;
    }

    // =========================================================
    // CREACIÓN DE LA SALIDA
    // =========================================================

    private GameObject CreateExitObject(
        Vector2Int tile
    )
    {
        GetWallPlacement(
            tile,
            out Vector3 localPosition,
            out Quaternion localRotation
        );

        localPosition.y = wallHeight * 0.5f;

        GameObject exitObject;

        if (exitPrefab != null)
        {
            exitObject =
                Instantiate(exitPrefab, transform);

            exitObject.name = "Hidden_Exit";

            exitObject.transform.localPosition =
                localPosition;

            exitObject.transform.localRotation =
                localRotation;
        }
        else
        {
            exitObject = CreateDefaultExit(
                localPosition,
                localRotation
            );
        }

        BackroomsExit exitScript =
            exitObject.GetComponent<BackroomsExit>();

        if (exitScript == null)
        {
            exitScript =
                exitObject.AddComponent<BackroomsExit>();
        }

        exitScript.manager = objectiveManager;

        BoxCollider trigger =
            exitObject.GetComponent<BoxCollider>();

        if (trigger == null)
        {
            trigger =
                exitObject.AddComponent<BoxCollider>();
        }

        trigger.isTrigger = true;

        trigger.center = new Vector3(
            0f,
            0f,
            0.5f
        );

        trigger.size = new Vector3(
            2.5f,
            wallHeight,
            1.5f
        );

        return exitObject;
    }

    private GameObject CreateDefaultExit(
        Vector3 localPosition,
        Quaternion localRotation
    )
    {
        GameObject root =
            new GameObject("Hidden_Exit");

        root.transform.SetParent(transform, false);
        root.transform.localPosition = localPosition;
        root.transform.localRotation = localRotation;

        GameObject door = CreatePrimitiveChild(
            "Exit_Door",
            PrimitiveType.Cube,
            root.transform,
            Vector3.zero,
            new Vector3(
                2.4f,
                wallHeight * 0.92f,
                0.15f
            ),
            exitMaterial
        );

        DisableCollider(door);

        GameObject frameTop = CreatePrimitiveChild(
            "Exit_Frame_Top",
            PrimitiveType.Cube,
            root.transform,
            new Vector3(
                0f,
                wallHeight * 0.5f,
                0f
            ),
            new Vector3(
                2.8f,
                0.15f,
                0.25f
            ),
            exitMaterial
        );

        GameObject frameLeft = CreatePrimitiveChild(
            "Exit_Frame_Left",
            PrimitiveType.Cube,
            root.transform,
            new Vector3(
                -1.3f,
                0f,
                0f
            ),
            new Vector3(
                0.15f,
                wallHeight,
                0.25f
            ),
            exitMaterial
        );

        GameObject frameRight = CreatePrimitiveChild(
            "Exit_Frame_Right",
            PrimitiveType.Cube,
            root.transform,
            new Vector3(
                1.3f,
                0f,
                0f
            ),
            new Vector3(
                0.15f,
                wallHeight,
                0.25f
            ),
            exitMaterial
        );

        DisableCollider(frameTop);
        DisableCollider(frameLeft);
        DisableCollider(frameRight);

        GameObject lightObject =
            new GameObject("Exit_Light");

        lightObject.transform.SetParent(root.transform, false);

        lightObject.transform.localPosition =
            new Vector3(0f, 0.6f, 0.5f);

        Light exitLight =
            lightObject.AddComponent<Light>();

        exitLight.type = LightType.Point;
        exitLight.intensity = 3f;
        exitLight.range = 8f;
        exitLight.color = Color.green;

        return root;
    }

    // =========================================================
    // POSICIONAMIENTO JUNTO A PAREDES
    // =========================================================

    private void GetWallPlacement(
        Vector2Int tile,
        out Vector3 position,
        out Quaternion rotation
    )
    {
        position = TileCenter(tile.x, tile.y);
        rotation = Quaternion.identity;

        Vector2Int wallDirection =
            GetAdjacentWallDirection(tile);

        if (wallDirection == Vector2Int.zero)
            return;

        Vector3 direction3D = new Vector3(
            wallDirection.x,
            0f,
            wallDirection.y
        ).normalized;

        // Se coloca ligeramente antes de la pared,
        // dentro del espacio caminable.
        position += direction3D * (tileSize * 0.46f);

        // El frente del objeto apunta hacia el jugador.
        rotation = Quaternion.LookRotation(
            -direction3D,
            Vector3.up
        );
    }

    private bool HasAdjacentWall(Vector2Int tile)
    {
        return GetAdjacentWallDirection(tile)
               != Vector2Int.zero;
    }

    private Vector2Int GetAdjacentWallDirection(
        Vector2Int tile
    )
    {
        List<Vector2Int> wallDirections =
            new List<Vector2Int>();

        foreach (Vector2Int direction in directions)
        {
            int neighborX = tile.x + direction.x;
            int neighborZ = tile.y + direction.y;

            if (!IsInsideGrid(neighborX, neighborZ))
            {
                wallDirections.Add(direction);
                continue;
            }

            if (!openTiles[neighborX, neighborZ])
            {
                wallDirections.Add(direction);
            }
        }

        if (wallDirections.Count == 0)
        {
            return Vector2Int.zero;
        }

        return wallDirections[
            Random.Range(0, wallDirections.Count)
        ];
    }

    // =========================================================
    // UTILIDADES
    // =========================================================

    private Vector3 TileCenter(int x, int z)
    {
        float originX =
            -(gridWidth - 1) * tileSize * 0.5f;

        float originZ =
            -(gridDepth - 1) * tileSize * 0.5f;

        return new Vector3(
            originX + x * tileSize,
            0f,
            originZ + z * tileSize
        );
    }

    private bool IsInsideGrid(int x, int z)
    {
        return
            x >= 0 &&
            x < gridWidth &&
            z >= 0 &&
            z < gridDepth;
    }

    private GameObject CreateBlock(
        string objectName,
        Vector3 localPosition,
        Vector3 localScale,
        Material material
    )
    {
        GameObject block =
            GameObject.CreatePrimitive(PrimitiveType.Cube);

        block.name = objectName;

        block.transform.SetParent(transform, false);
        block.transform.localPosition = localPosition;
        block.transform.localRotation = Quaternion.identity;
        block.transform.localScale = localScale;

        if (material != null)
        {
            Renderer renderer =
                block.GetComponent<Renderer>();

            renderer.sharedMaterial = material;
        }

        block.isStatic = true;

        return block;
    }

    private GameObject CreatePrimitiveChild(
        string objectName,
        PrimitiveType primitiveType,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Material material
    )
    {
        GameObject createdObject =
            GameObject.CreatePrimitive(primitiveType);

        createdObject.name = objectName;

        createdObject.transform.SetParent(parent, false);
        createdObject.transform.localPosition = localPosition;
        createdObject.transform.localRotation = Quaternion.identity;
        createdObject.transform.localScale = localScale;

        if (material != null)
        {
            Renderer renderer =
                createdObject.GetComponent<Renderer>();

            renderer.sharedMaterial = material;
        }

        return createdObject;
    }

    private void DisableCollider(GameObject target)
    {
        Collider targetCollider =
            target.GetComponent<Collider>();

        if (targetCollider != null)
        {
            targetCollider.enabled = false;
        }
    }

    private void RemoveCollider(GameObject target)
    {
        Collider targetCollider =
            target.GetComponent<Collider>();

        if (targetCollider == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(targetCollider);
        }
        else
        {
            Destroy(targetCollider);
        }
#else
        Destroy(targetCollider);
#endif
    }

    // =========================================================
    // LIMPIAR GENERACIÓN ANTERIOR
    // =========================================================

    private void ClearOldWorld()
    {
        for (
            int i = transform.childCount - 1;
            i >= 0;
            i--
        )
        {
            GameObject child =
                transform.GetChild(i).gameObject;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(child);
            }
            else
            {
                Destroy(child);
            }
#else
            Destroy(child);
#endif
        }
    }
}