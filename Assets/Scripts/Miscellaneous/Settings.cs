using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Settings
{
    #region DUNGEON BUILD SETTING
    public const int maxDungeonRebuildAttemptsForRoomGraph = 100;
    public const int maxDungeonBuildAttempts = 10;
    #endregion  DUNGEON BUILD SETTING

    #region ROOM SETTING
    // Max number of child corridors leading from a room,
    // max should be 3 although this isn't recommended
    // since it can cause the dungeon building to fail
    // since the room are more likely to not fit together
    public const int maxChildCorridors = 3;
    #endregion ROOM SETTING


    #region ANIMATOR PARAMETERS
    
    // Animator parameters - Player
    public static int aimUp = Animator.StringToHash("aimUp");
    public static int aimDown = Animator.StringToHash("aimDown");
    public static int aimUpRight = Animator.StringToHash("aimUpRight");
    public static int aimUpLeft = Animator.StringToHash("aimUpLeft");
    public static int aimRight = Animator.StringToHash("aimRight");
    public static int aimLeft = Animator.StringToHash("aimLeft");
    public static int isIdle = Animator.StringToHash("isIdle");
    public static int isMoving = Animator.StringToHash("isMoving");
    
    #endregion ANIMATOR PARAMETERS
}
