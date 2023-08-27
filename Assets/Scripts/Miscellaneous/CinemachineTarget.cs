using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(CinemachineTargetGroup))]
public class CinemachineTarget : MonoBehaviour
{
    private CinemachineTargetGroup cinemachineTargetGroup;
    private void Awake()
    {
        // Load Component
        cinemachineTargetGroup = GetComponent<CinemachineTargetGroup>();
    }

    void Start()
    {
        SetChinemachineTargetGroup();
            
    }
    private void SetChinemachineTargetGroup()
    {
        CinemachineTargetGroup.Target cinemachineGroupTarget_player = new CinemachineTargetGroup.Target { weight = 1f, radius = 1f, target = GameManager.Instance.GetPlayer().transform };

        CinemachineTargetGroup.Target[] cinemachineTargetArray = new CinemachineTargetGroup.Target[] { cinemachineGroupTarget_player };

        cinemachineTargetGroup.m_Targets = cinemachineTargetArray;
    }
}
