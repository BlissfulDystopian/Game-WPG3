using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent (typeof(MovementByVelocityEvent))]
[DisallowMultipleComponent]

public class MovementByVelocity : MonoBehaviour
{
    private Rigidbody2D rigidbody2D;
    private MovementByVelocityEvent movementByVelocityEvent;

    private void Awake()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();
        movementByVelocityEvent = GetComponent<MovementByVelocityEvent>();
    }

    private void OnEnable()
    {
        // Subscribe to movement event
        movementByVelocityEvent.onMovementByVelocity += MovementByVelocityEvent_OnMovementByVelocity;
    }

    private void OnDisable()
    {
        // Unsubscribe from movement event
        movementByVelocityEvent.onMovementByVelocity -= MovementByVelocityEvent_OnMovementByVelocity;
    }

    private void MovementByVelocityEvent_OnMovementByVelocity(MovementByVelocityEvent movementByVelocityEvent, MovementByVelocityArgs movementByVelocityArgs)
    {
        MoveRigidBody(movementByVelocityArgs.moveDirection, movementByVelocityArgs.moveSpeed);
    }
    
    /// <summary>
    /// Move the rigidbody component 
    /// </summary>
    private void MoveRigidBody(Vector2 moveDirection, float moveSpeed)
    {
        // Ensure the rb collision detection is set to continuous
        rigidbody2D.velocity = moveDirection * moveSpeed;
    }
}
