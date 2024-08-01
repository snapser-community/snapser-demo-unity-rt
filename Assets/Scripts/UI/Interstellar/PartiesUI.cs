
using System.ComponentModel.DataAnnotations;
using UnityEngine;
using UnityEngine.UI;

namespace Snapser.UI
{
   public class PartiesUI : MonoBehaviour
   {
      [SerializeField, Required] private Button deletePartyButton, queuePartyButton, leavePartyButton, dequeuePartyButton, updateMetadataButton;
      
      private SnapserNetworkManager _snapserNetworkManager;

      private void Start()
      {
         _snapserNetworkManager = SnapserNetworkManager.singleton;
      }

      private void Update()
      {
         if (_snapserNetworkManager.PartiesHandler == null)
         {
            return;
         }

         if (_snapserNetworkManager.PartiesHandler.CurrentParty != null)
         {
            if (_snapserNetworkManager.PartiesHandler.CurrentParty.Owner == _snapserNetworkManager.userId)
            {
               // leavePartyButton.interactable = false;
               deletePartyButton.interactable = true;
               queuePartyButton.interactable = true;
               dequeuePartyButton.interactable = true;
            }
            else
            {
               deletePartyButton.interactable = false;
               queuePartyButton.interactable = false;
               dequeuePartyButton.interactable = false;
               // leavePartyButton.interactable = true;
            }

            updateMetadataButton.interactable = true;
            leavePartyButton.interactable = true;
         }
         else
         {
            deletePartyButton.interactable = false;
            queuePartyButton.interactable = false;
            dequeuePartyButton.interactable = false;
            // leavePartyButton.interactable = false;
         }
      }
      
      public void OnLeavePartyButtonPressed()
      {
         Debug.Log("Leave Party Button Pressed");
         _snapserNetworkManager.PartiesHandler.LeaveParty();
      }

      public void OnUpdateMetadataButtonPressed()
      {
         Debug.Log("Update Metadata Button Pressed");
         _snapserNetworkManager.PartiesHandler.UpdatePlayerMetadata();
      }
   }
}