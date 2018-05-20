﻿using MLAPI;
using MLAPI.MonoBehaviours.Core;
using System.Collections.Generic;
using UnityEngine;

public class Proximity : NetworkedBehaviour {

    public enum CheckMethod
    {
        Physics3D,
        Physics2D
    };

    [TooltipAttribute("The maximum range that objects will be visible at.")]
    public int visRange = 10;

    [TooltipAttribute("How often (in seconds) that this object should update the set of players that can see it.")]
    public float visUpdateInterval = 1.0f; // in seconds

    [TooltipAttribute("Which method to use for checking proximity of players.\n\nPhysics3D uses 3D physics to determine proximity.\n\nPhysics2D uses 2D physics to determine proximity.")]
    public CheckMethod checkMethod = CheckMethod.Physics3D;

    [TooltipAttribute("Enable to force this object to be hidden from players.")]
    public bool forceHidden = false;

    float m_VisUpdateTime;

    void Update()
    {
        if (!isServer)
            return;

        if (Time.time - m_VisUpdateTime > visUpdateInterval)
        {
           RebuildObservers();
            m_VisUpdateTime = Time.time;
        }
    }

    // called when a new player enters
    public override bool OnCheckObserver(uint newClientId)
    {
        if (forceHidden)
            return false;

        Vector3 pos = NetworkingManager.singleton.ConnectedClients[newClientId].PlayerObject.transform.position;
        return (pos - transform.position).magnitude < visRange;
    }

    public override bool OnRebuildObservers(HashSet<uint> observers)
    {
        if (forceHidden)
        {
            // ensure player can still see themself
            var uv = networkedObject;
            if (uv != null && (uv.isPlayerObject))
            {
                observers.Add(uv.OwnerClientId);
            }
            return true;
        }

        // find players within range
        switch (checkMethod)
        {
            case CheckMethod.Physics3D:
                {
                    var hits = Physics.OverlapSphere(transform.position, visRange);
                    for (int i = 0; i < hits.Length; i++)
                    {
                        var hit = hits[i];
                        // (if an object has a connectionToClient, it is a player)
                        var uv = hit.GetComponent<NetworkedObject>();
                        if (uv != null && (uv.isPlayerObject))
                        {
                            observers.Add(uv.OwnerClientId);
                        }
                    }
                    return true;
                }

            case CheckMethod.Physics2D:
                {
                    var hits = Physics2D.OverlapCircleAll(transform.position, visRange);
                    for (int i = 0; i < hits.Length; i++)
                    {
                        var hit = hits[i];
                        // (if an object has a connectionToClient, it is a player)
                        var uv = hit.GetComponent<NetworkedObject>();
                        if (uv != null && (uv.isPlayerObject))
                        {
                            observers.Add(uv.OwnerClientId);
                        }
                    }
                    return true;
                }
        }
        return false;
    }
}
