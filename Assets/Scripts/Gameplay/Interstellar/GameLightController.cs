using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Mirror;
using UnityEngine;
using UnityEngine.Events;
using Utilities;

public class GameLightController : NetworkBehaviour
{
    [SerializeField, Required] private GameLightControllerVariable gameLightControllerVariable;
    [SerializeField, Required] private SpriteRenderer light;
    [SerializeField] float timerMin = 15f, timerMax = 30f;
    [SerializeField] private float delayEventTimer = 0.3f;
    [SerializeField] Color stopColor = Color.red, goColor = Color.green, warningColor = Color.yellow;
    
    //On currentGameLightColorState value changed, OnGameLightColorChange function is called on all clients, including the host
    [SyncVar(hook = nameof(OnGameLightColorChange))] 
    private Color currentGameLightColorState;
    
    public bool IsGameLightStateGo => currentGameLightColorState == goColor;
    public bool IsGameLightStateStop => currentGameLightColorState == stopColor;
    public bool IsGameLightStateWarning => currentGameLightColorState == warningColor;
    private float finishLinePositionThreshold;
    public float FinishLinePositionThreshold => finishLinePositionThreshold;
    
    private float currentTimeElapsed = 0f;
    private float currentTimer;
    
    private void Start()
    {
        finishLinePositionThreshold = transform.position.x;
        gameLightControllerVariable.Value = this;
        currentGameLightColorState = stopColor;
        currentTimer = Random.Range(timerMin, timerMax);
    }

    private void FixedUpdate()
    {
        CalculateTimeElapsed();
    }

    //Sync hook function variable currentGameLightColorState
    //On currentGameLightColorState value changed, OnGameLightColorChange function is called on all clients, including the host
    private void OnGameLightColorChange(Color oldColor, Color newColor)
    {
        light.color = newColor;
    }
    
    //Main logic function for the game light's color switching.
    //Functions with 'ServerCallback' tags are executed only on the server/host
    [ServerCallback]
    private void CalculateTimeElapsed()
    {
        currentTimeElapsed += Time.fixedDeltaTime;
        if (currentTimeElapsed >= currentTimer)
        {
            currentTimeElapsed = 0f;
            if (IsGameLightStateGo)
            {
                currentTimer = delayEventTimer;
                currentGameLightColorState = warningColor;
            }
            else if (IsGameLightStateWarning)
            {
                currentTimer = Random.Range(timerMin, timerMax);
                currentGameLightColorState = stopColor;
            }
            else if (IsGameLightStateStop)
            {
                currentTimer = Random.Range(timerMin, timerMax);
                currentGameLightColorState = goColor;
            }
        }
    }
}