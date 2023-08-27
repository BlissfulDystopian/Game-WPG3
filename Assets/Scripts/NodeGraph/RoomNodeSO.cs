using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.PlasticSCM.Editor.WebApi;

public class RoomNodeSO : ScriptableObject
{
    //  Core member variable

    [HideInInspector] public string id;
    [HideInInspector] public List<string> parentRoomNodeIDList = new List<string>();
    [HideInInspector] public List<string> childRoomNodeIDList = new List<string>();
    [HideInInspector] public RoomNodeGraphSO roomNodeGraph;
    public RoomNodeTypeSO roomNodeType;
    [HideInInspector] public RoomNodeTypeListSO roomNodeTypeList;

    #region Editor Code

    // The following code should only be run in the Unity Editor
#if UNITY_EDITOR

    [HideInInspector] public Rect rect;
    [HideInInspector] public bool isLeftClickDragging = false;
    [HideInInspector] public bool isSelected = false;

    // Initialize node
    public void Initialize(Rect rect, RoomNodeGraphSO nodeGraph, RoomNodeTypeSO roomNodeType)
    {
        this.rect = rect;
        this.id = Guid.NewGuid().ToString();
        this.name = "RoomNode";
        this.roomNodeGraph = nodeGraph;
        this.roomNodeType = roomNodeType;

        //  Load room node type list
        roomNodeTypeList = GameResources.Instance.roomNodeTypeList;
    }
    
    // Draw node with nodeStyle
    public void Draw(GUIStyle nodeStyle)
    {
        // Draw Node box using begin area
        GUILayout.BeginArea(rect, nodeStyle);

        // start region to detect popup selection changes
        EditorGUI.BeginChangeCheck();

        // if the room node has a parent or is a type of enterance then display a label else display popup
        if(parentRoomNodeIDList.Count > 0 || roomNodeType.isEnterance)
        {
            EditorGUILayout.LabelField(roomNodeType.roomNodeTypeName);
        }
        else
        {
            //  Display a popup using the roomNodeType name values that can be selected from
            int selected = roomNodeTypeList.list.FindIndex(x => x == roomNodeType);
            int selection = EditorGUILayout.Popup("", selected, GetRoomNodeTypesToDisplay());
            roomNodeType = roomNodeTypeList.list[selection];

            // if the room type selection has changed making child connection potentially invalid
            if (roomNodeTypeList.list[selected].isCorridor && !roomNodeTypeList.list[selection].isCorridor
                || !roomNodeTypeList.list[selected].isCorridor && roomNodeTypeList.list[selection].isCorridor
                || !roomNodeTypeList.list[selected].isBossRoom && roomNodeTypeList.list[selection].isBossRoom)
            {
                if (childRoomNodeIDList.Count > 0)
                {
                    for (int i = childRoomNodeIDList.Count - 1; i >= 0; i--)
                    {
                        // Get child room node
                        RoomNodeSO childRoomNode = roomNodeGraph.GetRoomNode(childRoomNodeIDList[i]);

                        // if the child room node is not null
                        if (childRoomNode != null)
                        {
                            // remove childID from parent room node
                            RemoveChildRoomNodeIDFromRoomNode(childRoomNode.id);
                            
                            // remove parentID from child room node
                            childRoomNode.RemoveParentRoomNodeIDFromRoomNode(id);
                        }
                    }
                }
            }
        }
            
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(this);
        }
        GUILayout.EndArea();

    }
    public string[] GetRoomNodeTypesToDisplay()
    {
        string[] roomArray = new string[roomNodeTypeList.list.Count];

        for(int i = 0; i < roomNodeTypeList.list.Count; i++)
        {
            if (roomNodeTypeList.list[i].displayInNodeGraphEditor)
            {
                roomArray[i] = roomNodeTypeList.list[i].roomNodeTypeName;
            }
        }
        return roomArray;
    }
    public void ProcessEvents(Event currentEvent)
    {
        switch (currentEvent.type)
        {
            // When we click it
            case EventType.MouseDown :
                ProcessMouseDownEvent(currentEvent);
                break;
            // When we release it
            case EventType.MouseUp :
                ProcessMouseUpEvent(currentEvent);
                break;
            // When we drag it
            case EventType.MouseDrag :
                ProcessMouseDragEvent(currentEvent);
                break;
            default:
                break;
        }
    }

    private void ProcessMouseDownEvent(Event currentEvent)
    {
        // 0 represent LMB, 1 represent RMB
        if(currentEvent.button == 0)
        {
            ProcessLeftMouseDownEvent();
        }
        if(currentEvent.button == 1)
        {
            ProcessRightMouseDownEvent(currentEvent);
        }
    }
    
    private void ProcessLeftMouseDownEvent()
    {
        Selection.activeObject = this;

        // Toggle node selection
        isSelected = !isSelected;

    }
    private void ProcessRightMouseDownEvent(Event currentEvent)
    {
        roomNodeGraph.SetNodeToDrawConnectionLineFrom(this, currentEvent.mousePosition);
    }
    private void ProcessMouseUpEvent(Event currentEvent)
    {
        // Left Mouse Uo event
        if(currentEvent.button == 0)
        {
            ProcessLeftMouseUpEvent();
        }
    }
    private void ProcessLeftMouseUpEvent()
    {
        if (isLeftClickDragging)
        {
            isLeftClickDragging = false;
        }
    }
    private void ProcessMouseDragEvent(Event currentEvent)
    {
        // left click drag event
        if(currentEvent.button == 0)
        {
            ProcessLeftMouseDragEvent(currentEvent);
        }
    }
    private void ProcessLeftMouseDragEvent(Event currentEvent)
    {
        isLeftClickDragging = true;
        DragNode(currentEvent.delta);
        GUI.changed = true;
    }
    public void DragNode(Vector2 delta)
    {
        rect.position += delta;
        EditorUtility.SetDirty(this);
    }
    // Check child node can be validly added to the parent node -
    // return true if it can otherwise return false
    public bool IsChildRoomValid(string childID)
    {
        bool isConnectedBossNodeAlready = false;

        foreach(RoomNodeSO roomNode in roomNodeGraph.roomNodeList)
        {
            // check if there is already a connection to the boss room in the node graph
            if(roomNode.roomNodeType.isBossRoom && roomNode.parentRoomNodeIDList.Count > 0)
            {
                isConnectedBossNodeAlready = true;
            }
        }
        
        // if child node has type of boss room and there is already a connected boss room then return false
        if(roomNodeGraph.GetRoomNode(childID).roomNodeType.isBossRoom && isConnectedBossNodeAlready)
           return false;        

        // if the child node has a type of none then return false
        if(roomNodeGraph.GetRoomNode(childID).roomNodeType.isNone)
            return false;

        // if the node already has a this child id, it return false
        if(childRoomNodeIDList.Contains(childID))
            return false;

        // if this childID is already in the parentID list return false
        if(parentRoomNodeIDList.Contains(childID))
            return false;

        // if the child node already has a parent return false
        if(roomNodeGraph.GetRoomNode(childID).parentRoomNodeIDList.Count > 0)
            return false;

        // if the child is a corridor and this node is a corridor return false
        if(roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && roomNodeType.isCorridor)
            return false;

        // if they both room then return false
        if(!roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && !roomNodeType.isCorridor)
            return false;

        // if adding a coridor check that this node has < max permitted child corridor
        if(roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && childRoomNodeIDList.Count >= Settings.maxChildCorridors)
            return false;

        // if the child room is an enterance return false (enterance must be an ultra mega parentnihbozzz anjayy)
        if(roomNodeGraph.GetRoomNode(childID).roomNodeType.isEnterance)
            return false;

        // if adding a room to a corridor check that this corridor node doesn't have a room added already
        if(!roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && childRoomNodeIDList.Count > 0)
            return false;
        
        return true;
    }
    public bool AddChildRoomNodeIDToRoomNode(string childID)
    {
        if (IsChildRoomValid(childID))
        {
            childRoomNodeIDList.Add(childID);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Add parentID in the node, return true if the node has been added. False otherwise
    /// </summary>
    public bool AddParentRoomNodeIDToRoomNode(string parentID)
    {
        parentRoomNodeIDList.Add(parentID);
        return true;
    }

    /// <summary>
    /// Remove Child ID from the node 
    /// return true if the node has been removed, false otherwise
    /// </summary>
    public bool RemoveChildRoomNodeIDFromRoomNode(string childID)
    {
        // if the node contains the child ID then remove it
        if (childRoomNodeIDList.Contains(childID))
        {
            childRoomNodeIDList.Remove(childID);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Remove Parent ID from the node 
    /// return true if the node has been removed, false otherwise
    /// </summary>
    public bool RemoveParentRoomNodeIDFromRoomNode(string parentID)
    {
        // if the node contains the parent ID then remove it
        if (parentRoomNodeIDList.Contains(parentID))
        {
            parentRoomNodeIDList.Remove(parentID);
            return true;
        }
        return false;
    }

#endif
    #endregion
}
