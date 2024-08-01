using System;
using Snapser.Handlers;
using UnityEngine;

namespace Snapser
{
    public class GameServerFleetsHandler
    {
        public void OnGameServerStateUpdated(object sender, OnGsfGameServerStateUpdatedArgs args)
        {
            Debug.Log("Game server state updated: " + args.GameServerName + " " + args.PreviousState + " --> " + args.NewState);
        }
    }
}