using System;
using System.Collections.Generic;
using UnityEditor.Categorization;
using UnityEngine;



public class ProceduralAnim : MonoBehaviour
{
    
    [SerializeField] Transform tailParent;

    [SerializeField] float maxTheta = 10;


    [SerializeField] Transform targetTransform;

    LinkedList<Transform> tailList;

    void Start()
    {
        tailList = TraverseAndAssign();
    }

    void Update()
    {
        pointToTarget(tailList);
    }


    LinkedList<Transform> TraverseAndAssign() {
        LinkedList<Transform> tailList = new LinkedList<Transform>();
        tailList.AddLast(tailParent);
        
        TraverseRecursive(tailParent, tailList);
        
        return tailList;
    }

    void TraverseRecursive(Transform currentParent, LinkedList<Transform> list) {
        if (currentParent.childCount == 0 || currentParent.childCount == 2) {
            return;
        }

        foreach (Transform child in currentParent) {
            list.AddLast(child);
            TraverseRecursive(child, list);
        }
    }

    void pointToTarget(LinkedList<Transform> tailList) {
        Vector3 targetPos = targetTransform.position;
        LinkedListNode<Transform> curr = tailList.Last;

        while(curr != null) {
            Vector3 currPos = curr.Value.position;
            Vector3 dirToTarget = (targetPos - currPos).normalized;

            Quaternion targetRotation = Quaternion.FromToRotation(curr.Value.up, dirToTarget) * curr.Value.rotation;

            curr.Value.rotation = Quaternion.RotateTowards(curr.Value.rotation, targetRotation, maxTheta);

            curr = curr.Previous;
        }
    }
}