using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public class DungeonBuilder : SingletonMonoBehaviour<DungeonBuilder>
{
    public Dictionary<string, Room> dungeonBuilderRoomDictionary = new Dictionary<string, Room>();

    private Dictionary<string, RoomTemplateSO> roomTemplateDictionary = new Dictionary<string, RoomTemplateSO>();

    private List<RoomTemplateSO> roomTemplateList = null;

    private RoomNodeTypeListSO roomNodeTypeList;

    private bool dungeonBuildSuccessful;

    protected override void Awake()
    {
        base.Awake();

        // Load the room node type list
        LoadRoomNodeTypeList();

        // Set dimmed material to fully visible
        GameResources.Instance.dimmedMaterial.SetFloat("Alpha_Slider", 1f);
    }


    public bool GenerateDungeon(DungeonLevelSO currentDungeonLevel)
    {
        roomTemplateList = currentDungeonLevel.roomTemplateList;

        // Load the scriptable object room templates into the dictionary
        LoadRoomTemplatesIntoDictionary();

        dungeonBuildSuccessful = false;
        int dungeonBuildAttempts = 0;

        while(!dungeonBuildSuccessful && dungeonBuildAttempts < Settings.maxDungeonBuildAttempts)
        {
            dungeonBuildAttempts++;

            // Select a random room node graph from the list
            RoomNodeGraphSO roomNodeGraph = SelectRandomRoomNodeGraph(currentDungeonLevel.roomNodeGraphList);

            int dungeonRebuildAttemptsForNodeGraph = 0;
            dungeonBuildSuccessful = false;

            // Loop until dungeon successfully built=d=t or more than max attempts for node graph
            while(!dungeonBuildSuccessful && dungeonRebuildAttemptsForNodeGraph <= Settings.maxDungeonRebuildAttemptsForRoomGraph)
            {
                // Clear dungeon room gameObjects and dungeon room dictionary
                ClearDungeon();

                dungeonRebuildAttemptsForNodeGraph++;

                // Attempt to build a random dungeon for the selected room node graph
                dungeonBuildSuccessful = AttemptToBuildRandomDungeon(roomNodeGraph);

                if (dungeonBuildSuccessful)
                {
                    // Instantiate room gameObjects
                    InstantiateRoomGameObjects();
                }
            }
        }
        return dungeonBuildSuccessful;
    }




    /// <summary>
    /// Load the room node type list
    /// </summary>
    private void LoadRoomNodeTypeList()
    {
        roomNodeTypeList = GameResources.Instance.roomNodeTypeList;
    }

    private void LoadRoomTemplatesIntoDictionary()
    {
        // Clear room template dictionary
        roomTemplateDictionary.Clear();

        // Load room template list into dictionary
        foreach(RoomTemplateSO roomTemplate in roomTemplateList)
        {
            if (!roomTemplateDictionary.ContainsKey(roomTemplate.guid))
            {
                roomTemplateDictionary.Add(roomTemplate.guid, roomTemplate);
            }
            else
            {
                Debug.Log("Duplicate Room Template Key In " + roomTemplateList);
            }
        }

    }

    /// <summary>
    /// Attempt to randomly  build the dungeon for the specified room node graph. Returns true if a 
    /// successful random layout was generated, else return false if a problem was encountered and
    /// another attempt is required.
    /// </summary>


    private bool AttemptToBuildRandomDungeon(RoomNodeGraphSO roomNodeGraph)
    {
        // Create open room node queue
        Queue<RoomNodeSO> openRoomNodeQueue = new Queue<RoomNodeSO>();

        // Add enterance node to room node queue from room node graph
        RoomNodeSO enteranceNode = roomNodeGraph.GetRoomNode(roomNodeTypeList.list.Find(x => x.isEnterance));

        if(enteranceNode != null)
        {
            openRoomNodeQueue.Enqueue(enteranceNode);
        }
        else
        {
            Debug.Log("No enterance Node");
            return false;
        }

        // Start with no room overlaps
        bool noRoomOverlaps = true;

        // Process open room nodes queue
        noRoomOverlaps = ProcessRoomInOpenRoomNodeQueue(roomNodeGraph, openRoomNodeQueue, noRoomOverlaps);

        return (openRoomNodeQueue.Count == 0 && noRoomOverlaps) ? true : false;
        
    }


    /// <summary>
    /// Process rooms in the open room node queue, returning true if there are no room overlaps
    /// </summary>
    private bool ProcessRoomInOpenRoomNodeQueue(RoomNodeGraphSO roomNodeGraph, Queue<RoomNodeSO> openRoomNodeQueue, bool noRoomOverlaps)
    {

        // while room nodes in open room node queue & no room overlaps
        while(openRoomNodeQueue.Count > 0 && noRoomOverlaps == true)
        {
            // Get next room node from open room node queue
            RoomNodeSO roomNode = openRoomNodeQueue.Dequeue();

            // add child nodes to queue from room node graph (with links to this parent room)
            foreach(RoomNodeSO childRoomNode in roomNodeGraph.GetChildRoomNodes(roomNode))
            {
                openRoomNodeQueue.Enqueue(childRoomNode);
            }

            // if room node is the enterance mark as positioned and add to room dictionary
            if (roomNode.roomNodeType.isEnterance)
            {
                RoomTemplateSO roomTemplate = GetRandomRoomTemplate(roomNode.roomNodeType);

                Room room = CreateRoomFromRoomTemplate(roomTemplate, roomNode);

                room.isPositioned = true;

                // add room to room dictionary
                dungeonBuilderRoomDictionary.Add(room.id, room);
                
            }
            // else if the room type isn't an enterance
            else
            {
                // Else get parent room for node
                Room parentRoom = dungeonBuilderRoomDictionary[roomNode.parentRoomNodeIDList[0]];

                // See if room can be placed without overlaps
                noRoomOverlaps = CanPlaceRoomWithNoOverlaps(roomNode, parentRoom);
            }
        }
        return noRoomOverlaps;
    }

    /// <summary>
    /// 
    /// </summary>
    private bool CanPlaceRoomWithNoOverlaps(RoomNodeSO roomNode, Room parentRoom)
    {
        // initialize and assume overlap until proven otherwise
        bool roomOverlaps = true;

        // Do while room overlaps - try to place against all available doorway of the parent untul
        // the room is successfully placed without overlap
        while (roomOverlaps)
        {
            // Select random unconnected available doorway for parent
            List<Doorway> unconnectedavailableParentDoorways = GetUnconnectedAvailableDoorways(parentRoom.doorwayList).ToList<Doorway>();
        
            if (unconnectedavailableParentDoorways.Count == 0)
            {
                // if no more doorways to try then overlap failure
                return false;
            }

            Doorway doorwayParent = unconnectedavailableParentDoorways[UnityEngine.Random.Range(0, unconnectedavailableParentDoorways.Count)];

            // Get a random room template for room node that is consistent with the parent door orientation
            RoomTemplateSO roomTemplate = GetRandomTemplateForRoomConsistentWithParent(roomNode, doorwayParent);
        
            Room room = CreateRoomFromRoomTemplate(roomTemplate, roomNode);

            // Place the room - returns true if the room doesn't overlap
            if (PlaceTheRoom(parentRoom, doorwayParent, room))
            {
                // if room  doesn't overlap then set to false to exit while loop
                roomOverlaps = false;

                // Mark room as positioned
                room.isPositioned = true;

                // Add room to dictionary
                dungeonBuilderRoomDictionary.Add(room.id, room);
            }
            else roomOverlaps = true;
        }
        return true; // No room overlap

    }

    /// <summary>
    /// Get Random room template for room node that is consistent with the parent door orientation 
    /// </summary>
    private RoomTemplateSO GetRandomTemplateForRoomConsistentWithParent(RoomNodeSO roomNode, Doorway doorwayParent)
    {
        RoomTemplateSO roomTemplate = null;
        
        // if room node is a corridor then select random correct Corridor room template based on
        // parent doorway orientation
        if(roomNode.roomNodeType.isCorridor)
        {
            switch (doorwayParent.orientation)
            {
                case Orientation.north:
                case Orientation.south:
                    roomTemplate = GetRandomRoomTemplate(roomNodeTypeList.list.Find(x => x.isCorridorNS));
                    break;

                case Orientation.east:
                case Orientation.west:
                    roomTemplate = GetRandomRoomTemplate(roomNodeTypeList.list.Find(x => x.isCorridorEW));
                    break;

                case Orientation.none:
                    break;

                default:
                    break;
            }
        }
        else
        {
            roomTemplate = GetRandomRoomTemplate(roomNode.roomNodeType);
        }

        return roomTemplate;
    }

    /// <summary>
    /// 
    /// </summary>
    private bool PlaceTheRoom(Room parentRoom, Doorway doorwayParent, Room room)
    {
        // Get current room doorway position
        Doorway doorway = GetOppositeDoorway(doorwayParent, room.doorwayList);

        // return if no doorway in room opposite to parent doorway
        if (doorway == null)
        {
            // just mark the parent doorway as unavailable so we dont try and connect it again
            doorwayParent.isUnavailable = true;

            return false;
        }

        // Calculate world grid parent doorway position
        Vector2Int parentDoorwayPosition = parentRoom.lowerBounds + doorwayParent.position - parentRoom.templateLowerBound;

        Vector2Int adjustment = Vector2Int.zero;

        // Calculate adjustment position offset based on room doorway position that we're trying to connect 
        // (e.g. if this doorway is west then we need to add (1, 0) to the east parent doorway)
        switch (doorway.orientation)
        {
            case Orientation.north:
                adjustment = new Vector2Int(0, -1);
                break;

            case Orientation.east:
                adjustment = new Vector2Int(-1, 0);
                break;
            
            case Orientation.south:
                adjustment = new Vector2Int(0, 1);
                break;

            case Orientation.west:
                adjustment = new Vector2Int(1, 0);
                break;

            case Orientation.none:
                break;

            default:
                break;

        }

        // Calculate room lower bounds and upper bounds based on positioning to align with parent doorway
        room.lowerBounds = parentDoorwayPosition + adjustment + room.templateLowerBound - doorway.position;
        room.upperBounds = room.lowerBounds + room.templateUpperBound - room.templateLowerBound;

        Room overlappingRoom = CheckForRoomOverlap(room);

        if(overlappingRoom == null)
        {
            // mark doorway as connected & unavailable
            doorwayParent.isConnected = doorwayParent.isUnavailable = true;
            doorway.isConnected = doorway.isUnavailable = true;

            // return true to show rooms have been connected with no overlap
            return true;
        }
        else
        {
            // Just mark the parent doorway as unavailable so we don't try and connect it again
            doorwayParent.isUnavailable = true;

            return false;
        }
    }

    /// <summary>
    /// Get the doorway from the doorway list that has the opposite orientation to doorway 
    /// </summary>
    private Doorway GetOppositeDoorway(Doorway ParentDoorway, List<Doorway> doorwayList)
    {
        foreach(Doorway doorwayToCheck in doorwayList)
        {
            if(ParentDoorway.orientation == Orientation.east && doorwayToCheck.orientation == Orientation.west)
            {
                return doorwayToCheck;
            }
            else if(ParentDoorway.orientation == Orientation.west && doorwayToCheck.orientation == Orientation.east)
            {
                return doorwayToCheck;
            }
            else if(ParentDoorway.orientation == Orientation.north && doorwayToCheck.orientation == Orientation.south)
            {
                return doorwayToCheck;
            }
            else if(ParentDoorway.orientation == Orientation.south && doorwayToCheck.orientation == Orientation.north)
            {
                return doorwayToCheck;
            }
        }
        return null;
    }


    ///<summary>
    /// Checks for rooms that overlap the upper and lower bounds parameters, and if there are overlapping rooms then return
    /// rooms else return null
    /// </summary>
    private Room CheckForRoomOverlap(Room roomToTest)
    {
        // Iterate through all rooms
        foreach (KeyValuePair<string, Room> keyvaluepair in dungeonBuilderRoomDictionary){
            Room room = keyvaluepair.Value;
            
            // Skip if same room as room to test or room hasn't been positioned
            if(room.id == roomToTest.id || !room.isPositioned) 
            {
                continue;
            }
            if(IsOverlappingRoom(roomToTest, room))
            {
                return room;
            }
        }

        return null;
    }

    private bool IsOverlappingRoom(Room room1, Room room2)
    {
        bool isOverlappingX = isOverlappingInterval(room1.lowerBounds.x, room1.upperBounds.x, room2.lowerBounds.x, room2.upperBounds.x);
        
        bool isOverlappingY = isOverlappingInterval(room1.lowerBounds.y, room1.upperBounds.y, room2.lowerBounds.y, room2.upperBounds.y);

        return (isOverlappingX && isOverlappingY) ? true : false;
    
    }
    
    
    /// <summary>
    /// Check if interval 1 overlaps interval 2 - this method is used by the IsOverlappingRoom method
    /// </summary>
    private bool isOverlappingInterval(int imin1, int imax1, int imin2, int imax2)
    {
        return (Mathf.Max(imin1, imin2) <= Mathf.Min(imax1, imax2)) ? true : false;
    }



    /// <summary>
    /// Select a random room node graph from the list of room node graphs
    /// </summary>
    private RoomTemplateSO GetRandomRoomTemplate(RoomNodeTypeSO roomNodeType)
    {
        List<RoomTemplateSO> matchingRoomTemplateList = new List<RoomTemplateSO>();

        // Loop through room template list
        foreach(RoomTemplateSO roomTemplate in roomTemplateList)
        {
            // Add matching room templates
            if(roomTemplate.roomNodeType == roomNodeType)
            {
                matchingRoomTemplateList.Add(roomTemplate);
            }
        }

        // return null if list is zero
        if (matchingRoomTemplateList.Count == 0)
            return null;

        // Select random room template form list and return
        return matchingRoomTemplateList[UnityEngine.Random.Range(0, matchingRoomTemplateList.Count)];

    }


    ///<summary>
    ///
    /// </summary>
    private IEnumerable<Doorway> GetUnconnectedAvailableDoorways(List<Doorway> roomDoorwayList)
    {
        //Loop through doorway list
        foreach (Doorway doorway in roomDoorwayList)
        {
            if(!doorway.isConnected && !doorway.isUnavailable)
            {
                yield return doorway;
            }
        }
    }
    /// <summary>
    /// Create room based on roomTemplate and layoutNode, and return the created one
    /// </summary>
    private Room CreateRoomFromRoomTemplate(RoomTemplateSO roomTemplate, RoomNodeSO roomNode)
    {
        // Initialize room from template
        Room room = new Room();

        room.templateID = roomTemplate.guid;
        room.id = roomNode.id;
        room.prefab = roomTemplate.prefab;
        room.roomNodeType = roomTemplate.roomNodeType;
        room.lowerBounds = roomTemplate.lowerBounds;
        room.upperBounds = roomTemplate.upperBounds;
        room.spawnPositionArray = roomTemplate.spawnPositionArray;
        room.templateLowerBound = roomTemplate.lowerBounds;
        room.templateUpperBound = roomTemplate.upperBounds;
        room.childRoomIDList = CopyStringList(roomNode.childRoomNodeIDList);
        room.doorwayList = CopyDoorwayList(roomTemplate.doorwayList);

        // Set parent ID for room
        if(roomNode.parentRoomNodeIDList.Count == 0) // Enterance
        {
            room.parentRoomID = "";
            room.isPreviouslyVisited = true;

            // Set enterance in game manager
            GameManager.Instance.SetCurrentRoom(room);
        }
        else
        {
            room.parentRoomID = roomNode.parentRoomNodeIDList[0];
        }
        return room;

    }

    /// <summary>
    /// Select a random room node graph from the list of room node graphs
    /// </summary>
    private RoomNodeGraphSO SelectRandomRoomNodeGraph(List<RoomNodeGraphSO> roomNodeGraphList)
    {
        if(roomNodeGraphList.Count > 0)
        {
            return roomNodeGraphList[UnityEngine.Random.Range(0, roomNodeGraphList.Count)];
        }
        else
        {
            Debug.Log("No room node graph in list");
            return null;
        }
    }
    /// <summary>
    /// Create deep copu of doorway list 
    /// </summary>
    private List<Doorway> CopyDoorwayList(List<Doorway> oldDoorWayList)
    {
        List<Doorway> newDoorWayList = new List<Doorway>();

        foreach(Doorway doorway in oldDoorWayList)
        {
            Doorway newDoorWay = new Doorway();

            newDoorWay.position = doorway.position;
            newDoorWay.orientation = doorway.orientation;
            newDoorWay.doorPrefab = doorway.doorPrefab;
            newDoorWay.isConnected = doorway.isConnected;
            newDoorWay.isUnavailable = doorway.isUnavailable;
            newDoorWay.doorwayStartCopyPosition = doorway.doorwayStartCopyPosition;
            newDoorWay.doorwayCopyTileWidth = doorway.doorwayCopyTileWidth;
            newDoorWay.doorwayCopyTileHeight = doorway.doorwayCopyTileHeight;

            newDoorWayList.Add(newDoorWay);
        }
        return newDoorWayList;
    }

    /// <summary>
    /// Create deep copy of string list 
    /// </summary>
    private List<string> CopyStringList(List<string> oldStringList)
    {
        List<string> newStringList = new List<string>();

        foreach(string stringValue in oldStringList)
        {
            newStringList.Add(stringValue);
        }

        return newStringList;
    }

    /// <summary>
    /// Instantiate the dungeon room gameObjects from the prefabs
    /// </summary>
    private void InstantiateRoomGameObjects()
    {
        foreach (KeyValuePair<string, Room> keyvaluepair in dungeonBuilderRoomDictionary)
        {
            Room room = keyvaluepair.Value;

            // Calculate room position (Remember the room instantiation position 
            // needs to be adjusted by the room template lower bounds)
            Vector3 roomPosition = new Vector3(room.lowerBounds.x - room.templateLowerBound.x, room.lowerBounds.y - room.templateLowerBound.y, 0f);

            // Instantiate room
            GameObject roomGameObject = Instantiate(room.prefab, roomPosition, Quaternion.identity, transform);

            // Get Instantiated room component from instantiated prefab
            InstantiatedRoom instantiatedRoom = roomGameObject.GetComponentInChildren<InstantiatedRoom>();

            instantiatedRoom.room = room;

            // Initialize the instantiated room
            instantiatedRoom.Initialize(roomGameObject);

            // Save gameObject reference
            room.instantiatedRoom = instantiatedRoom;

        }
    }

    /// <summary>
    /// Get a room template by room template ID, returns null if ID doesn't exist
    /// </summary>
    public RoomTemplateSO GetRoomTemplate(string roomTemplateID)
    {
        if (roomTemplateDictionary.TryGetValue(roomTemplateID, out RoomTemplateSO roomTemplate))
        {
            return roomTemplate;
        }
        else return null;
    }

    /// <summary>
    /// Get room by roomID, if no room exist with that ID return null
    /// </summary>
    public Room GetRoomByRoomID(string roomID)
    {
        if (dungeonBuilderRoomDictionary.TryGetValue(roomID, out Room room))
        {
            return room;
        }
        else return null;
    }

    /// <summary>
    /// Clear dungeon room gameObjects and dungeon room dictionary
    /// </summary>
    private void ClearDungeon()
    {
        // Destroy initiated dungeon gameObjects and clear dungeon manager room dictionary
        if(dungeonBuilderRoomDictionary.Count > 0)
        {
            foreach(KeyValuePair<string, Room> keyValuePair in dungeonBuilderRoomDictionary)
            {
                Room room = keyValuePair.Value;

                if(room.instantiatedRoom != null)
                {
                    Destroy(room.instantiatedRoom.gameObject);
                }
            }
            dungeonBuilderRoomDictionary.Clear();
        }
    }

}
