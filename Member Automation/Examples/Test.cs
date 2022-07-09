using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tom.Automation.Runtime;

public class Test : AutomatedMonoBehaviour
{
    [Assign(AssignEvent.Awake, GetMode.Self)]public Rigidbody rigidBody; //Calls GetComponent on the member in Awake.
    [Assign(AssignEvent.Start, value: 100)] public int integer; //Sets the value of the member to the value specified in Start.
    
    //Works on properties as well.
    [Assign(AssignEvent.Enable, GetMode.Child)] public CharacterController Controller { get; set; } //Calls GetComponentInChildren in OnEnable.
    [Assign(AssignEvent.Enable, GetMode.Self, Order = 0)] public Transform ObjectTransform { get; set; } //Execution order can be set by changing the Order property.

    [Call(AssignEvent.Start, Order = 0)] //Invoke the method on start.
    public void MethodTest() {
    }

    [Call(AssignEvent.Update, 12, "String", Order = 1)] //Invoke the method in Update with parameters.
    public void MethodTest2(int i, string s) { 
    }
}
