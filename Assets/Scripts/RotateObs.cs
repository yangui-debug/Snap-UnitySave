using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateObs : MonoBehaviour
{
    public int force; // 力度大小
    
    private void OnCollisionEnter(Collision other)
    {
	Debug.Log("1");
        if (other.gameObject.tag.Equals("Player"))
        {
		Debug.Log(other.contactCount );
            if (other.contactCount > 0)
            {
                var otherContact = other.contacts[0];
                var forceDir = (other.transform.position - otherContact.point).normalized;
Debug.Log(forceDir);
                var rigidbody = other.gameObject.GetComponent<Rigidbody>();
                if (rigidbody != null)
                {
			Debug.Log("3");
                    rigidbody.AddForce(forceDir * force,ForceMode.Impulse);
                }else{
Debug.Log("4");
		}
            }

            
        }
    }
}
