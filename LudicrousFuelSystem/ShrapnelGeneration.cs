using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP;
using static UnityEngine.Random;

namespace LudicrousFuelSystem
{
    public class ExplosionParticle : MonoBehaviour
    {
        Rigidbody rb;
        physicalObject thisObj;
        public float drag;
        public float mass;
        public Vector3 iniVel;
        public float timeToLive;
        void Start()
        {
            if (rb == null)
                rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.velocity = iniVel;
            rb.useGravity = true;
            if (thisObj == null)
                thisObj = gameObject.AddComponent<physicalObject>();
            thisObj.maxDistance = 100f;
            thisObj.origDrag = drag;
            thisObj.rb = rb;
            FlightGlobals.addPhysicalObject(thisObj);
        }
        void HandlePhysicsAnyway()
        {
            rb.AddForce(FlightGlobals.getGeeForceAtPosition(transform.position), ForceMode.Acceleration);
            Vector3d vel = rb.velocity + Krakensbane.GetFrameVelocity();
            float alt = FlightGlobals.getAltitudeAtPos(transform.position);
            rb.AddForce(-vel.magnitude * vel * drag * 0.001f * FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(alt), FlightGlobals.getExternalTemperature(alt)), ForceMode.Force);
        }
        void Update()
        {
            if (PauseMenu.isOpen)
                return;
            timeToLive -= TimeWarp.deltaTime;
            if (timeToLive < 0f)
                Destroy(gameObject);
            if (FlightGlobals.ActiveVessel.state == Vessel.State.DEAD)
                HandlePhysicsAnyway();
        }
    }

    static class ShrapnelGeneration
    {
        public static void SpawnShrapnel(Part p, int pieces, float explodeViolence)
        {
            pieces = Mathf.Clamp(pieces, 1, 70);
            string name = p.partInfo.title;
            if (KSP.Localization.Localizer.CurrentLanguage == "fr-fr")
                name = ConfigInfo.shrapnelName + name + ")";
            else
                name += ConfigInfo.shrapnelName;

            float shrapnelSize = Mathf.Pow(p.mass / pieces, 0.333333f);
            float explodeStr = Mathf.Sqrt(explodeViolence / p.mass * (pieces - 1) / pieces) * 45f;
            for (int i = 0; i < pieces; i++)
            {
                GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                g.transform.localScale = new Vector3(Range(0.1f, 1f), Range(0.1f, 1f), Range(0.1f, 1f));
                g.transform.localScale *= shrapnelSize / Mathf.Pow(g.transform.localScale.x * g.transform.localScale.y * g.transform.localScale.z + Range(0f, .5f), 0.333333333333333f);
                g.name = name;
                g.transform.position = p.transform.position + onUnitSphere;
                ExplosionParticle ex = g.AddComponent<ExplosionParticle>();
                ex.drag = g.transform.localScale.sqrMagnitude * 0.2f;
                ex.mass = g.transform.localScale.x * g.transform.localScale.y * g.transform.localScale.z;
                ex.timeToLive = Range(2f, 45f);
                ex.iniVel = insideUnitSphere * explodeStr + p.rb.velocity;
            }
        }

        internal static void SpawnShrapnel(Part part, int v1, double v2)
        {
            throw new NotImplementedException();
        }
    }
}
