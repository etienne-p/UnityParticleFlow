using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ParticleFlow
{
    [ExecuteInEditMode]
    public class Node : Sampler
    {
        [Serializable]
        public class FloatProperty
        {
            public string name;
            public float value;
            public float rangeMin;
            public float rangeMax;

            public FloatProperty(string name_, float value_ = .0f, float rangeMin_ = .0f, float rangeMax_ = 1.0f)
            {
                name = name_;
                value = value_;
                rangeMin = rangeMin_;
                rangeMax = rangeMax_;
            }
        }

        [Serializable]
        public class PropertyGroup
        {
            public string name;
            public FloatProperty[] properties;

            public PropertyGroup(string name_, FloatProperty[] properties_)
            {
                name = name_;
                properties = properties_;
            }
        }

        public PropertyGroup[] propertyGroups;
        public Color color;
        public UnityEvent OnChanged;

        [HideInInspector]
        public bool isDirty = true;
        
        Dictionary<string, Dictionary<string, FloatProperty>> propertiesGroupsDict;
        Dictionary<string, PropertyGroup> groupsDict;

        void Awake()
        {
            if (propertyGroups == null)
                InitializeProperties();
            InitializeDict();
            OnChanged = new UnityEvent();
        }

        void OnDestroy()
        {
            OnChanged.RemoveAllListeners();    
        }

        void Update()
        {
            if (transform.hasChanged)
            {
                isDirty = true;
                transform.hasChanged = false;
            }
        }

        public void OnValidate()
        {
            isDirty = true;
            OnChanged.Invoke();
        }

        void OnDrawGizmos()
        {
            Vector3 from = transform.TransformPoint(Vector3.zero);
            Vector3 to = from + transform.TransformVector(transform.forward * .1f);
            Gizmos.DrawSphere(from, .05f);
            Gizmos.DrawLine(from, to);
        }

        [ContextMenu("Reset")]
        void Reset()
        {
            InitializeProperties();
            InitializeDict();
        }

        void InitializeProperties()
        {
            FloatProperty[] attractorParms = new FloatProperty[3]; 
            attractorParms[0] = new FloatProperty("radius");
            attractorParms[1] = new FloatProperty("power", 0, 0, 4);
            attractorParms[2] = new FloatProperty("strength", 0, -6, 6);

            FloatProperty[] beamParms = new FloatProperty[3]; 
            beamParms[0] = new FloatProperty("radius");
            beamParms[1] = new FloatProperty("power", 0, 0, 4);
            beamParms[2] = new FloatProperty("strength", 0, -6, 6);

            FloatProperty[] twirlParms = new FloatProperty[4]; 
            twirlParms[0] = new FloatProperty("radius");
            twirlParms[1] = new FloatProperty("power", 0, 0, 4);
            twirlParms[2] = new FloatProperty("strength", 0, -6, 6);
            twirlParms[3] = new FloatProperty("angle", 0, -180, 180);

            propertyGroups = new PropertyGroup[3];
            propertyGroups[0] = new PropertyGroup("Attractor", attractorParms);
            propertyGroups[1] = new PropertyGroup("Beam", beamParms);
            propertyGroups[2] = new PropertyGroup("Twirl", twirlParms);    
        }

        public void Lerp(Node a, Node b, float ratio)
        {
            for (int i = 0; i < propertyGroups.Length; ++i)
            {
                for (int j = 0; j < propertyGroups[i].properties.Length; ++j)
                {
                    propertyGroups[i].properties[j].value = Mathf.Lerp(
                        a.propertyGroups[i].properties[j].value, 
                        b.propertyGroups[i].properties[j].value, ratio);
                }
            }
            color = Color.Lerp(a.color, b.color, ratio);
        }

        override public void Sample(Vector3 position, out Vector3 velocity, out Color color_)
        {
            velocity = SampleAttractor(position) + SampleBeam(position) + SampleTwirl(position);
            color_ = color;
        }

        Vector3 SampleAttractor(Vector3 position)
        {
            return Util.Attractor(position, transform.localPosition, 
                propertyGroups[0].properties[0].value, 
                propertyGroups[0].properties[1].value, 
                propertyGroups[0].properties[2].value);
        }

        Vector3 SampleBeam(Vector3 position)
        {  
            return Util.Beam(position, transform.localPosition, 
                transform.forward, 
                propertyGroups[1].properties[0].value, 
                propertyGroups[1].properties[1].value, 
                propertyGroups[1].properties[2].value);
        }

        Vector3 SampleTwirl(Vector3 position)
        {
            return Util.Twirl(position, transform.localPosition, transform.forward, 
                propertyGroups[2].properties[0].value, 
                propertyGroups[2].properties[1].value, 
                propertyGroups[2].properties[2].value,
                propertyGroups[2].properties[3].value);
        }

        void InitializeDict()
        {
            groupsDict = new Dictionary<string, PropertyGroup>();
            propertiesGroupsDict = new Dictionary<string, Dictionary<string, FloatProperty>>();
            foreach (var group in propertyGroups)
            {
                groupsDict.Add(group.name, group);
                propertiesGroupsDict.Add(group.name, AsDict(group.properties));
            }
        }

        static Dictionary<string, FloatProperty> AsDict(FloatProperty[] parms)
        {
            var rv = new Dictionary<string, FloatProperty>();
            foreach (var i in parms)
            {
                rv.Add(i.name, i);
            }
            return rv;
        }
    }
}