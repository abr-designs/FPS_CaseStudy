﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody)), RequireComponent(typeof(Collider)), RequireComponent(typeof(View)), RequireComponent(typeof(KillableBase))]
public class EnemyAI : StateMachineBase
{
    ////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////

    private List<Transform> activeTargets;
    
    ////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////
    
    protected string[] AnimationParameters = new string[]
    {
        "Idle",
        "Running",
        "Shooting"
    };

    protected int[] parameterIds;
    
    ////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////
    
    protected override void Init()
    {
        //Set the Animator parameters to their respective Hash Codes
        parameterIds = new int[AnimationParameters.Length];
        for (int i = 0; i < AnimationParameters.Length; i++)
        {
            parameterIds[i] = Animator.StringToHash(AnimationParameters[i]);
        }

        GetComponent<KillableBase>().onHitCallback += Hit;
        
        InitState(STATE.IDLE);
    }

    protected override void InitState(STATE newState, object parameters = null)
    {
        //Sets the gameObject name to better debug in-editor
        
        currentState = newState;
        
        name = string.Format("[{0}]{1}", currentState, startingName);

        //Reset any timer that we have so we dont have any conflicts
        mTimer = 0f;

        switch (currentState)
        {
            case STATE.IDLE:
                break;
            case STATE.RUN:
                //Pick a point between current location and max view distance
                navMeshAgent.SetDestination((Vector3?) parameters ?? RandomNavmeshLocation(view.MaxViewDistance));
                break;
            case STATE.ATTACK:
                navMeshAgent.SetDestination(transform.position);
                break;
            case STATE.DEAD:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        SetAnimationState();
    }

    protected override void SetAnimationState()
    {
        animator.SetBool(parameterIds[0], false);
        animator.SetBool(parameterIds[1], false);
        animator.SetBool(parameterIds[2], false);
        
        switch (currentState)
        {
            case STATE.IDLE:
                animator.SetBool(parameterIds[0], true);
                break;
            case STATE.RUN:
                animator.SetBool(parameterIds[1], true);
                break;
            case STATE.ATTACK:
                animator.SetBool(parameterIds[2], true);
                break;
            case STATE.DEAD:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void LateUpdate()
    {
        view.CanSeeTargets(out activeTargets);
    }

    ////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////

    #region States
    
    protected override void IdleState()
    {
        if (mTimer >= idleWaitTime)
        {
            //TODO Need to move to a new run state
            InitState(STATE.RUN);
        }
        else
        {
            mTimer += Time.deltaTime;
        }
    }

    protected override void RunState()
    {
        if (activeTargets != null && activeTargets.Count > 0)
        {
            InitState(STATE.ATTACK);
            return;
        }

        if (navMeshAgent.remainingDistance < 0.2f)
        {
            InitState(STATE.IDLE);
            return;
        }
    }

    protected override void AttackState()
    {
        if (activeTargets == null || activeTargets.Count == 0)
        {
            InitState(STATE.RUN);
            return;
        }

        Vector3 lookAtTarget = activeTargets[0].position;
        lookAtTarget.y = transform.position.y;
        

        transform.forward = (lookAtTarget - transform.position).normalized;

        //TODO Need to chase player, without getting too close
        //TODO Shoot at player
    }

    protected override void DeadState()
    {
        throw new System.NotImplementedException();
    }
    
    #endregion //States

    void Hit(Vector3 fromPosition)
    {
        if (currentState == STATE.ATTACK)
            return;

        InitState(STATE.RUN, GetNavMeshPointFromPosition(fromPosition));
    }
    
    //Thanks to http://answers.unity.com/answers/1426690/view.html
    protected Vector3 RandomNavmeshLocation(float radius) 
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += transform.position;
        NavMeshHit hit;
        Vector3    finalPosition = Vector3.zero;
        if (NavMesh.SamplePosition(randomDirection, out hit, radius, 1)) {
            finalPosition = hit.position;            
        }
        return finalPosition;
    }

    protected Vector3 GetNavMeshPointFromPosition(Vector3 position)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(position, out hit, 5f, 1))
            return hit.position;

        return position;
    }

    private void OnDestroy()
    {
        GetComponent<KillableBase>().onHitCallback -= Hit;
    }
}
