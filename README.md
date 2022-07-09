# tuuump
Tom's Ultra Unity Utility Mega Pack
<h1>This is the repository for my new project, the <b>Ultra Unity Utility Mega Pack</b></h1>
<p>I will update the feature list as I add new features.</p>
<p>You are free to use all assets within this repository in all your projects, however please do not redistribute any of them as your own. (Not like you'd want to. C:)

<h1>Current Features</h1>
<ul>
  <li><b>Property Group Extension for Unity (<i>Made in Unity 2019.4.17f1</i>)</b> - Simple Inspector script for organising fields into groups.</li>
  <li><b>Attribute-based Member Automation (<i>Made in Unity 2019.4.17f1</i>)</b> - A runtime reflection utility used to initialise and call members throughout the GameObject life cycle.</li>
  <li><b>More to come!</b></li>
</ul>

<h1>Usage</h1>
<h2>Property Group Extension</h2>
<h3>Installation</h3>
<p>Just drag in the Property Group Extension folder into your assets folder and you're good to go!</p>

<h3>Demonstration</h3>

```csharp
using Tom.PropertyGroups.Runtime;

public class TestMonoBehaviour : MonoBehaviour {
  [PropertyGroup("Group Label")] public float number;
}
```
<img alt="In-Editor Preview" src="https://i.imgur.com/3UCib4p.gif"/>

<h2>Attribute-based Member Automation</h2>
<h3>Installation</h3>
<p>Just drag in the Member Automation folder into your assets folder and you're good to go!</p>

<h3>Demonstration</h3>

```csharp
using Tom.Automation.Runtime;

public class TestMonoBehaviour : AutomatedMonoBehaviour {
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
```

<h1>Credits</h1>
<ul>
  <li>None yet...</li>
</ul>

