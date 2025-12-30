using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AITarget : MonoBehaviour
{
    public Transform Target;
    public float RunningDistance;

    private NavMeshAgent m_Agent;
    private Animator m_Animator;
    private float m_Distance;

    // Start is called before the first frame update
    void Start()
    {
        m_Agent = GetComponent<NavMeshAgent>();
        m_Animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        m_Distance = Vector3.Distance(m_Agent.transform.position, Target.position);
        if (m_Distance < RunningDistance)
        {
            m_Agent.isStopped = true;
            m_Animator.SetBool("Running", true);
        }
        else
        {
            m_Agent.isStopped = false;
            m_Animator.SetBool("Running", false);
            m_Agent.destination = Target.position;
        }
    }

    void OnAnimatorMove()
    {
        if (m_Animator.GetBool("Running") == false)
        {
            m_Agent.speed = (m_Animator.deltaPosition / Time.deltaTime).magnitude;
        }
    }
}