using System;
using System.Reflection;
using UnityEngine;
using Tom.Automation.Reflection;
using System.Collections.Generic;

/// <summary>
/// AutomatedMonoBehaviour - 2022
/// Written by Tomasz Galka (tommy.galk@gmail.com) Github: GomysTalka
/// This class is part of [Tom's Ultra Unity Utility Mega Pack] which can be found on Github.
/// </summary>
namespace Tom.Automation.Runtime
{
    /// <summary>
    /// A custom <b>MonoBehaviour</b> class with automated Attribute-based functionality.
    /// </summary>
    public class AutomatedMonoBehaviour : MonoBehaviour
    {
        /// <summary>
        /// A dictionary which maps an event to a list of members.
        /// </summary>
        private readonly Dictionary<AssignEvent, List<Member>> _memberMap =
            new Dictionary<AssignEvent, List<Member>>();
        /// <summary>
        /// <para>A flag which determines whether <b>FindMembers()</b> has been invoked internally.</para>
        /// <para>If the flag is not set, all <b>AssignOrCall()</b> calls will fail. The
        /// flag is set at the beginning of the <b>Awake()</b> method. When overriding <b>Awake()</b>, 
        /// <em><b>base</b></em><b>.Awake()</b> must be called for this flag to be set.</para>
        /// This flag is set even if no members are found.
        /// </summary>
        private bool _membersFound;

        #region Unity Methods
        /// <summary>
        /// When overriding Awake(), base.Awake() needs to be called for automation to work!
        /// </summary>
        public virtual void Awake()
        {
            FindMembers();
            _membersFound = true;
            SortMembers();
            foreach (Member member in _memberMap[AssignEvent.Awake])
                AssignOrCall(member);
        }
        /// <summary>
        /// When overriding Start(), base.Start() needs to be called for automation to work!
        /// </summary>
        public virtual void Start()
        {
            foreach (Member member in _memberMap[AssignEvent.Start])
                AssignOrCall(member);
        }
        /// <summary>
        /// When overriding OnEnable(), base.OnEnable() needs to be called for automation to work!
        /// </summary>
        public virtual void OnEnable()
        {
            foreach (Member member in _memberMap[AssignEvent.Enable])
                AssignOrCall(member);
        }

        /// <summary>
        /// When overriding Update(), base.Update() needs to be called for automation to work!
        /// </summary>
        public virtual void Update()
        {
            if (_memberMap.ContainsKey(AssignEvent.Update))
            {
                foreach (Member member in _memberMap[AssignEvent.Update])
                    AssignOrCall(member);
            }
        }

        /// <summary>
        /// When overriding FixedUpdate(), base.FixedUpdate() needs to be called for automation to work!
        /// </summary>
        public virtual void FixedUpdate()
        {
            if (_memberMap.ContainsKey(AssignEvent.FixedUpdate))
            {
                foreach (Member member in _memberMap[AssignEvent.FixedUpdate])
                    AssignOrCall(member);
            }
        }

        /// <summary>
        /// When overriding LateUpdate(), base.LateUpdate() needs to be called for automation to work!
        /// </summary>
        public virtual void LateUpdate()
        {
            if (_memberMap.ContainsKey(AssignEvent.LateUpdate))
            {
                foreach (Member member in _memberMap[AssignEvent.LateUpdate])
                    AssignOrCall(member);
            }
        }
        #endregion

        #region Reflection
        /// <summary>
        /// Populates the member lists with all fields, properties and methods with Assign and Call attributes
        /// based on the assign event.
        /// </summary>
        private void FindMembers()
        {
            //Find all field and methods. (Public and Non Public)
            //BindingFlags.Instance = Search for instance members.
            //BindingFlags.Public = Search for public members.
            //BindingFlags.NonPublic = Search for non-public members.
            //BindingFlags.DeclaredOnly = Search for non-inherited members only.
            //Source: https://docs.microsoft.com/en-us/dotnet/api/system.reflection.bindingflags?view=net-6.0
            FieldInfo[] fields =
                GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo[] methods =
                GetType().GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            //Find the UnityEngine.CoreModule.dll
            Module unityModule = typeof(MonoBehaviour).Module;

            //Populate the member map with all possible events.
            for (byte i = 0; i < ((byte)AssignEvent.LateUpdate) + 1; i++)
                _memberMap.Add((AssignEvent)i, new List<Member>());

            //Create an array of indices for each event.
            int[] executionOrders = new int[_memberMap.Count];

            //Iterate over all found fields.
            for(int f = 0; f < fields.Length; f++)
            {
                FieldInfo field = fields[f];
                Type fieldType = field.FieldType;
                MemberType memberType = MemberType.Field;
                //Attempt to retrieve the AssignAttribute from the member.
                AssignAttribute assignAttr = (AssignAttribute)GetAttributeAndSetType(typeof(AssignAttribute), field, ref memberType);
                if (assignAttr == null) continue;

                //If the order has not been set by the user, set the order to the index of the field
                //bound to the particular event.
                if (assignAttr.Order == -1)
                    assignAttr.Order = executionOrders[(byte)assignAttr.AssignOn]++;

                //If the attribute has been found, add the member to the member list in
                //the map corresponding to the bound event. (AssignEvent)
                _memberMap[assignAttr.AssignOn].Add(
                    new Member(field, assignAttr.AssignOn, assignAttr, fieldType, memberType));
            }

            //Re-define the execution orders.
            executionOrders = new int[executionOrders.Length];

            //Iterate over all found methods.
            for (int m = 0; m < methods.Length; m++)
            {
                MethodInfo method = methods[m];
                //Exclude built-in Unity methods.
                if (method.Module == unityModule) continue;
                MemberType memberType = MemberType.Method;
                //Attempt to retrieve the CallAttribute from the method.
                CallAttribute callAttr = (CallAttribute)GetAttributeAndSetType(typeof(CallAttribute), method, ref memberType);
                if (callAttr == null) continue;

                //If the order has not been set by the user, set the order to the index of the method
                //bound to the particular event.
                if (callAttr.Order == -1)
                    callAttr.Order = executionOrders[(byte)callAttr.AssignOn]++;

                //If the attribute has been found, add the method to the member list in
                //the map corresponding to the bound event. (AssignEvent)
                _memberMap[callAttr.AssignOn].Add(
                        new Member(method, callAttr.AssignOn, callAttr, null, memberType));
            }
        }

        /// <summary>
        /// Sort all the members within the member map lists according to their execution order.
        /// </summary>
        private void SortMembers() {
            //Populate the member map with all possible events.
            for (byte i = 0; i < ((byte)AssignEvent.LateUpdate) + 1; i++)
            {
                AssignEvent assignEvent = (AssignEvent) i;
                if (!_memberMap.ContainsKey(assignEvent)) continue;
                List<Member> memberList = _memberMap[assignEvent];
                //Sorts members based on CompareTo() method of the Member class which
                //compares the execution orders.
                memberList.Sort();

                _memberMap[assignEvent] = memberList;
            }
        }

        /// <summary>
        /// <para>Assign or Call a member depending on whether it's a field or method.
        /// If the attribute's Value property wasn't set, <b>GetComponent()</b> is invoked with
        /// the field type.</para>
        /// <para>If the <b>Value</b> field of the attribute attached to the member is not of
        /// the same type as the member, an assertion error is logged and the value is not set.</para>
        /// <para>If the member type is not a Unity Component and a value has not been specified, a
        /// warning is logged and the value is not set.</para>
        /// </summary>
        /// <param name="member">The <b>Member</b> object containing information about the member.</param>
        private void AssignOrCall(Member member)
        {
            //Return immediately if the FindMembers() has not been invoked in Awake().
            if (!_membersFound) return;
            Type fieldType = member.FieldType;
            switch (member.MemberType)
            {
                //Treat Properties and Fields as the same as Properties' backing fields are found using
                //GetFields().
                case MemberType.Property:
                case MemberType.Field:
                    AssignAttribute assignAttr = (AssignAttribute)member.Attribute;

                    //Set flag if the field type is a valid Unity component.
                    bool isComponent = IsComponent(fieldType);
                    //Set flag if the optional attribute value is null or if the value's type matches the field type. 
                    bool typeAssert = assignAttr.Value == null || assignAttr.Value.GetType() == fieldType;
                    object o = null;

                    //Check if the value is null and the member is a valid Unity component.
                    //Try to get the component from the attached gameobject.
                    if (assignAttr.Value == null && isComponent)
                    {
                        //Determine which GetComponent method to call based on the GetMode of
                        //the attribute.
                        switch (assignAttr.GetMode) {
                            case GetMode.Child:
                                o = GetComponentInChildren(fieldType, true);
                                break;
                            case GetMode.Parent:
                                o = GetComponentInParent(fieldType);
                                break;
                            default:
                                o = GetComponent(fieldType);
                                break;
                        }
                    }
                    else
                    {
                        //Run an assertion on the previous typeAssert flag.
                        Debug.Assert(typeAssert, "Assertion Failed: Specified Value must be the same type as the member!" +
                            $" Member: [{fieldType}] | Value: [{assignAttr.Value.GetType()}]");
                        //Set the object to be assigned to the value of the attribute.
                        if (typeAssert)
                            o = assignAttr.Value;
                        else
                            o = null;
                    }

                    //Set the value of the member if the object to be assigned is not null.
                    if (o != null)
                        member.SetValue(this, o);
                    else
                    {
                        if (isComponent)
                            Debug.LogWarning($"There is no [{fieldType}] component attached to {member.MemberObject.Name}!");
                        else
                            Debug.LogWarning($"[{member.MemberObject.Name}:{fieldType}] A value must be specified when initialising non-component members!");
                    }
                    break;
                case MemberType.Method:
                    //Call the method.
                    member.Call(this);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Searches for an attribute attached to a specified <b>member</b>.
        /// </summary>
        /// 
        /// <param name="attributeType">The type of the attribute to be retrieved.</param>
        /// <param name="member">The <b>MemberInfo</b> object of the member.</param>
        /// <param name="memberType">Reference to the type of the member. This reference is only changed
        /// if the specified <b>member</b> is a property.</param>
        /// 
        /// <returns><para>Base <b>Attribute</b> object of the attribute tied to the member.</para>
        /// <para>If the <b>attributeType</b> is not a valid Attribute type,
        /// or if the member doesn't have the <b>attributeType</b> attribute attached, 
        /// <em><b>null</b></em> is returned.</para></returns>
        private Attribute GetAttributeAndSetType(Type attributeType, MemberInfo member, ref MemberType memberType)
        {
            //Try to get the attribute.
            Attribute attr = member.GetCustomAttribute(attributeType);
            //Check if the attribute is null and if the member is a property.
            //Property backing field names start with '<' and are in the format <TYPE>k__backingField.
            if (attr == null && member.Name[0] == '<')
            {
                string fName = member.Name;
                //This operation is required as backing fields do not contains
                //attribute information from the property.
                //Substring the string and retrieve the property name.
                fName = fName.Substring(1, fName.IndexOf('>') - 1);
                //Attempt to find the property by name.
                PropertyInfo prop =
                    GetType().GetProperty(fName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                //If the property has been found, set the member type to Property and
                //return the found attribute.
                if (prop != null)
                {
                    attr = prop.GetCustomAttribute(attributeType);
                    memberType = MemberType.Property;
                    if (attr == null) return null;
                    return attr;
                }
            }
            //Can be null.
            return attr;
        }

        /// <summary>
        /// Determines whether the specified <b>type</b> is a valid Unity component.
        /// </summary>
        /// <param name="type">The type to be tested.</param>
        /// <returns><b>true</b> if the type is a valid component, otherwise <b>false</b>.</returns>
        public bool IsComponent(Type type) => type.IsSubclassOf(typeof(Component));
        #endregion
    }

    #region Attribute Definitions
    /// <summary>
    /// <para>The Attribute used for event-based automated field and property assignment.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    class AssignAttribute : Attribute
    {
        /// <summary>
        /// The order of the member in the call hierarchy. -1 is default and will use the order
        /// in which the member is returned in using <b>GetFields()</b>.
        /// <para>This order is in an ascending order, meaning 0 will be executed first, 
        /// followed by the rest.</para>
        /// </summary>
        public int Order { get; set; } = -1;
        /// <summary>
        /// Event to bind to this member assignment.
        /// </summary>
        public AssignEvent AssignOn { get; private set; }
        /// <summary>
        /// Value to initialise the member with.
        /// </summary>
        public object Value { get; private set; } = null;

        /// <summary>
        /// <para>Specifies which <b>GetComponent</b> method to invoke on the member.</para> 
        /// <para>Ignored if member is a method or is not a component.</para>
        /// </summary>
        public GetMode GetMode { get; private set; }

        /// <summary>
        /// AssignAttribute Constructor.
        /// </summary>
        /// <param name="assignOn">Event to bind to this member assignment.</param>
        /// <param name="value">(Optional) Value to initialise the member with.</param>
        /// <param name="getMode">(Optional) Specifies which <b>GetComponent</b> method to invoke on the member.
        /// <b>GetMode.Self</b> is default.</param>
        public AssignAttribute(AssignEvent assignOn, GetMode getMode = GetMode.Self, object value = null)
        {
            AssignOn = assignOn;
            Value = value;
            GetMode = getMode;
        }
    }

    /// <summary>
    /// <para>The Attribute used for event-based automated method calls.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    class CallAttribute : Attribute
    {
        /// <summary>
        /// The order of the member in the call hierarchy. -1 is default and will use the order
        /// in which the member is returned in using <b>GetMethods()</b>.
        /// <para>This order is in an ascending order, meaning 0 will be executed first, 
        /// followed by the rest.</para>
        /// <para>This order is only valid for members bound to the same event!</para>
        /// </summary>
        public int Order { get; set; } = -1;
        /// <summary>
        /// Event to bind to this method call.
        /// </summary>
        public AssignEvent AssignOn { get; private set; }
        /// <summary>
        /// Parameters of the method to call as a params array of objects.
        /// </summary>
        public object[] Parameters { get; private set; }

        /// <summary>
        /// CallAttribute Constructor.
        /// </summary>
        /// <param name="assignOn">Event to bind to this method call.</param>
        /// <param name="parameters">Parameters of the method to call as a params array of objects.</param>
        public CallAttribute(AssignEvent assignOn, params object[] parameters)
        {
            AssignOn = assignOn;
            Parameters = parameters;
        }
    }
    #endregion

    #region Enumerations
    /// <summary>
    /// Enumeration for assignment and calling events.
    /// <b>AssignEvent.Awake</b> is default.
    /// </summary>
    public enum AssignEvent : byte
    {
        Awake,
        Start,
        Enable,
        Update,
        FixedUpdate,
        LateUpdate
    }

    /// <summary>
    /// Enumeration for determining <b>GetComponent</b> calls.
    /// <para><b>GetComponentInChildren()</b> is called with the
    /// <b><em>includeInactive</em></b> flag set.</para>
    /// <b>GetMode.Self</b> is default.
    /// </summary>
    enum GetMode : byte
    {
        Self, //GetComponent()
        Child, //GetComponentInChildren()
        Parent //GetComponentInParent()
    }
    #endregion
}

namespace Tom.Automation.Reflection
{
    using Tom.Automation.Runtime;

    /// <summary>
    ///Class used for storing information about the member, assigning and invoking members 
    ///through reflection.
    /// </summary>
    class Member : IComparable<Member>
    {
        /// <summary>
        /// <b>MemberInfo</b> object of the member.
        /// </summary>
        public MemberInfo MemberObject { get; private set; }
        /// <summary>
        /// Event to bind to this member call or assignment.
        /// </summary>
        public AssignEvent AssignOn { get; private set; }
        /// <summary>
        /// The attribute tied to this member.
        /// </summary>
        public Attribute Attribute { get; private set; }
        /// <summary>
        /// The field type of the member. This is <b><em>null</em></b> for methods.
        /// </summary>
        public Type FieldType { get; private set; }
        /// <summary>
        /// The type of the member.
        /// </summary>
        public MemberType MemberType { get; private set; }

        /// <summary>
        /// Returns the name of the member.
        /// </summary>
        public string Name => MemberObject.Name;

        /// <summary>
        /// Member Constructor
        /// </summary>
        /// <param name="mInfo">The <b>MemberInfo</b> object of the member.</param>
        /// <param name="assignEvent">Event to bind to this member call or assignment.</param>
        /// <param name="attribute">The attribute tied to this member.</param>
        /// <param name="fieldType">The field type of the member. This is <b><em>null</em></b> for methods.</param>
        /// <param name="type">The type of the member.</param>
        public Member(MemberInfo mInfo, AssignEvent assignEvent, Attribute attribute, Type fieldType, MemberType type = MemberType.Field)
        {
            MemberObject = mInfo;
            AssignOn = assignEvent;
            Attribute = attribute;
            MemberType = type;
            FieldType = fieldType;
        }

        /// <summary>
        /// Sets the value of the member if it is a field or a property.
        /// </summary>
        /// <param name="context">The member owner object.</param>
        /// <param name="value">The value to be set.</param>
        public void SetValue(object context, object value)
        {
            if (MemberObject == null) return;
            switch (MemberType)
            {
                case MemberType.Field:
                case MemberType.Property:
                    //If the member is a field or property cast the member object to
                    //a FieldInfo and invoke SetValue on the member to set the
                    //value to the specified value. Both fields and properties and
                    //fields are treated the same as properties are set through
                    //the internal backing field.
                    ((FieldInfo)MemberObject).SetValue(context, value);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// <para>Calls the member with parameters if it is a valid method.</para>
        /// <para>If the specified parameters don't match the method definition, 
        /// an assertion error is logged and the member is not invoked.</para> 
        /// </summary>
        /// <param name="context">The member owner object.</param>
        public void Call(object context)
        {
            if (MemberObject == null || MemberType != MemberType.Method) return;
            //Cast the base Attribute object to a CallAttribute.
            CallAttribute callAttr = (CallAttribute)Attribute;
            //Cast the MemberInfo object into a MethodInfo object.
            MethodInfo method = (MethodInfo)MemberObject;
            //Retrieve the parameters of the method.
            ParameterInfo[] paramInfo = method.GetParameters();

            //Invoke the method normally if it requires no parameters.
            if (paramInfo.Length == 0)
                method.Invoke(context, null);
            else
            {
                //Set an assertion flag if the specified parameter length matches or is greater than
                //the method definition's parameter length. Log an error otherwise and don't invoke 
                //the method.
                bool paramAssert = callAttr.Parameters.Length >= paramInfo.Length;
                Debug.Assert(paramAssert, "Assertion Failed: The parameter list must match the function definition!");
                if (!paramAssert) return;
                //Iterate over all parameters and check if all types match.
                for (int i = 0; i < paramInfo.Length; i++)
                {

                    if (callAttr.Parameters[i].GetType() != paramInfo[i].ParameterType)
                    {
                        Debug.LogWarning($"[{context.GetType()}:{method.Name}()]: The parameter list must match the function definition!");
                        return;
                    }
                }
                //Invoke the method with the specified parameters if all tests have passed.
                method.Invoke(context, callAttr.Parameters);
            }
        }

        /// <summary>
        /// Inherited from <b>IComparable</b> interface. Used for sorting based on execution order.
        /// </summary>
        /// <param name="other">The object to be compared to.</param>
        /// <returns><b>Integer</b> value determining whether the compared object is higher or lower.</returns>
        public int CompareTo(Member other)
        {
            if (other == null || other.Attribute == null) return 1;
            if (Attribute == null) return 0;
            return GetExecutionOrder().CompareTo(other.GetExecutionOrder());
        }

        /// <summary>
        /// Retrieves the execution order from the attribute tied to the member.
        /// </summary>
        /// <returns>The execution order index of the member.</returns>
        private int GetExecutionOrder() {
            //Return a default order if the attribute is null.
            if (Attribute == null) return 0;
            switch (MemberType)
            {
                case MemberType.Field:
                case MemberType.Property:
                    return ((AssignAttribute)Attribute).Order;
                default:
                    return ((CallAttribute)Attribute).Order;
            }
        }

        /// <summary>
        /// An implicit operator used for implicitly converting <b>MemberInfo</b> to a <b>Member</b>.
        /// </summary>
        /// <param name="mInfo">The <b>MemberInfo</b> object of the member.</param>
        public static implicit operator Member(MemberInfo mInfo)
            => new Member(mInfo, AssignEvent.Awake, null, null);
    }

    /// <summary>
    /// Enumeration for all possible member types. 
    /// <b>MemberType.Field</b> is default.
    /// </summary>
    enum MemberType : byte
    {
        Field,
        Property,
        Method
    }
}
