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
            thisObj.maxDistance = 1000f;
            thisObj.origDrag = drag;
            thisObj.rb = rb;
            FlightGlobals.addPhysicalObject(thisObj);
        }
        void HandlePhysicsAnyway()
        {
            rb.AddForce(FlightGlobals.getGeeForceAtPosition(transform.position), ForceMode.Acceleration);
            Vector3d vel = rb.velocity + Krakensbane.GetFrameVelocity();
            float alt = FlightGlobals.getAltitudeAtPos(transform.position);
            rb.AddForce(-vel.magnitude * vel * drag * 0.002f * FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(alt), FlightGlobals.getExternalTemperature(alt)), ForceMode.Force);
        }
        void FixedUpdate()
        {
            if (PauseMenu.isOpen)
                return;
            timeToLive -= TimeWarp.fixedDeltaTime;
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
            pieces = Mathf.Clamp(pieces, 1, 100);
            string name = ConfigInfo.shrapnelName + p.partInfo.title + ")";

            float shrapnelSize = Mathf.Pow(p.mass / pieces, 0.333333f);
            float explodeStr = Mathf.Sqrt(explodeViolence / p.mass * (pieces - 1) / pieces) * 15f;
            for (int i = 0; i < pieces; i++)
            {
                GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                g.transform.localScale = new Vector3(Range(0.1f, 1f), Range(0.1f, 1f), Range(0.1f, 1f));
                g.transform.localScale *= shrapnelSize / Mathf.Pow(g.transform.localScale.x * g.transform.localScale.y * g.transform.localScale.z + Range(0f, .03f), 0.333333333333333f);
                g.name = name;
                g.transform.position = p.transform.position + onUnitSphere;
                ExplosionParticle ex = g.AddComponent<ExplosionParticle>();
                ex.drag = g.transform.localScale.sqrMagnitude * 0.01f;
                ex.mass = g.transform.localScale.x * g.transform.localScale.y * g.transform.localScale.z;
                ex.timeToLive = Range(2f, 15f);
                ex.iniVel = insideUnitSphere * explodeStr + p.rb.velocity;
            }
        }
    }
}
