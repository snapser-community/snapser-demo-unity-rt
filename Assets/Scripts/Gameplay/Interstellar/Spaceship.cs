using System.Collections;
using System.ComponentModel.DataAnnotations;
using Mirror;
using Snapser;
using UnityEngine;
using UnityEngine.Events;
using Utilities;


public class Spaceship : NetworkBehaviour
{
    [SerializeField] float startSpeed = 30, victorySpeed = 1500;
    [SerializeField, Required] private GameLightControllerVariable gameLightControllerVariable;
    [SerializeField, Required] Rigidbody2D rigidbody2d;
    [SerializeField, Required] SpriteRenderer spriteRenderer;
    [SerializeField] private GameObject explosionEffect, victoryEffect, localPlayerIcon;
    [SerializeField] private float victoryEffectInterval = 0.8f, explosionImpactRadius = 20f, explosionForce = 400f;
    [SerializeField, Required] private LayerMask explosionLayer;
    [SerializeField] private bool isInDebugMode;
    
    //Sync variables to maintain player information on all clients
    //On userName value changed, InitializePlayer function is called on all clients, including the host
    [SyncVar(hook = nameof(InitializePlayer))]
    private string userName;
    
    //On playerColor value changed, OnColorChange function is called on all clients, including the host
    [SyncVar(hook = nameof(OnColorChange))]
    private Color playerColor;
    
    //Sync variables to maintain player state on all clients
    //On hasWon value changed, OnHasWonChanged function is called on all clients, including the host
    [SyncVar(hook = nameof(OnHasWonChanged))]
    private bool hasWon = false;
    
    //On isEliminated value changed, OnIsEliminatedChanged function is called on all clients, including the host
    [SyncVar(hook = nameof(OnIsEliminatedChanged))] 
    private bool isEliminated;

    //We maintain local variables to track player state along with the synced variables
    //for no latency on the local client
    private bool localHasWon = false, localIsEliminated;
    
    private float speed, victoryEffectTimeElapsed;
    
    public static event UnityAction<string, Color> OnUpdatePlayerUI;
    public static event UnityAction<string, bool> OnPlayerEliminated;
    public static event UnityAction<string, bool> OnPlayerWon;
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        speed = startSpeed;
        CmdInitializePlayer(SnapserNetworkManager.singleton.username);
        localPlayerIcon.SetActive(isLocalPlayer);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        
        //playerColor = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
        playerColor = SnapserNetworkManager.singleton.GetSpaceshipColor(gameObject);
    }

    public void InitializeSpaceship(Color assignedColor)
    {
        CmdInitializePlayer(SnapserNetworkManager.singleton.username);
        playerColor = assignedColor;
    }

    //Functions with 'Command' tag are executed only on the server
    //When the userName variable is set, it calls a sync hook function on all clients to synchronize.
    [Command]
    private void CmdInitializePlayer(string uName)
    {
        userName = uName;
    }
    
    private void InitializePlayer(string oldName, string newName)
    {
        OnUpdatePlayerUI?.Invoke(userName, playerColor);
    }
    
    //When the isEliminated variable is set, it calls a sync hook function on all clients to synchronize.
    [Command]
    private void CmdChangeIsEliminated(Vector3 eliminationPosition)
    {
        isEliminated = true;
        Explode(eliminationPosition);
    }
    
    private void OnIsEliminatedChanged(bool oldValue, bool newValue)
    {
        if (!localIsEliminated)
            OnIsEliminatedChanged();
    }

    private void OnIsEliminatedChanged()
    {
        if ((isLocalPlayer && localIsEliminated) || isEliminated)
        {
            OnPlayerEliminated?.Invoke(userName, isLocalPlayer);
            localPlayerIcon.SetActive(false);
            if (explosionEffect != null)
            {
                GameObject expEffect = Instantiate(explosionEffect, transform.position, Quaternion.identity);
                Destroy(expEffect, 2f);
            }
            
            StartCoroutine(DestroyPlayer());
            //transform.position = new Vector3(-1000, -1000, 0);
            spriteRenderer.enabled = false;
        }
    }
    
    //When the hasWon variable is set, it calls a sync hook function on all clients to synchronize.
    [Command]
    private void CmdChangePlayerHasWon()
    {
        hasWon = true;
    }
    
    private void OnHasWonChanged(bool oldValue, bool newValue)
    {
        if (localHasWon)
            return;
        
        OnHasWonChanged();
    }

    private void OnHasWonChanged()
    {
        if ((isLocalPlayer && localHasWon) || hasWon)
        {
            speed = victorySpeed;
            victoryEffectTimeElapsed = 0f;
            OnPlayerWon?.Invoke(userName, isLocalPlayer);
        }
    }
    
    private void OnColorChange(Color oldColor, Color newColor)
    {
        spriteRenderer.color = newColor;
        
        if (!string.IsNullOrEmpty(userName))
            OnUpdatePlayerUI?.Invoke(userName, playerColor);
    }

    //Functions with 'ClientRpc' tags are called on all clients
    [ClientRpc]
    private void Explode(Vector3 eliminationPosition)
    {
        Debug.Log("Exploding player " + userName + " at position "+ eliminationPosition +". Is local player : " + isLocalPlayer);
        Collider2D[] colls = Physics2D.OverlapCircleAll(eliminationPosition, explosionImpactRadius, explosionLayer);

        foreach (Collider2D coll in colls)
        {
            Vector2 direction = eliminationPosition - coll.transform.position;
            coll.GetComponent<Rigidbody2D>().AddForce(direction * explosionForce);
        }
    }

    IEnumerator DestroyPlayer()
    {
        yield return new WaitForSeconds(3f);
        NetworkServer.Destroy(gameObject);
    }

    //FixedUpdate for rigidbody
    void FixedUpdate()
    {
        // Only let the local player's spaceship be controlled by this client
        
        if (isLocalPlayer)
        {
            //The game light is instantiated after all the players have joined.
            //There can me a race condition where an update loop for some of the
            //early players will begin before the game light is instantiated from the server
            if (isEliminated || localIsEliminated || gameLightControllerVariable.Value == null)
            {
                return;
            }
            
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            
            //Has the player met all the conditions to be eliminated?
            if (!isInDebugMode && !hasWon && !localHasWon && gameLightControllerVariable.Value.IsGameLightStateStop && (!horizontal.Equals(0f) || !vertical.Equals(0f)))
            {
                Debug.Log("Player moved under red light. Eliminating player.");
                localIsEliminated = true;
                OnIsEliminatedChanged();
                CmdChangeIsEliminated(transform.position);
            }
            
            //Move the spaceship
            rigidbody2d.velocity = new Vector2(horizontal, vertical) * speed * Time.fixedDeltaTime;
            
            //Has the player met all the conditions to have won the level?
            if (!hasWon && !localHasWon && transform.position.x >= gameLightControllerVariable.Value.FinishLinePositionThreshold)
            {
                Debug.Log("Player won. Finish line x pos : " + gameLightControllerVariable.Value.FinishLinePositionThreshold + " and player x pos : " + transform.position.x);
                localHasWon = true;
                OnHasWonChanged();
                CmdChangePlayerHasWon();
            }
        }
        
        //Play particle effects if the player has won
        if (hasWon)
        {
            victoryEffectTimeElapsed += Time.fixedDeltaTime;
            if (victoryEffectTimeElapsed >= victoryEffectInterval)
            {
                victoryEffectTimeElapsed = 0f;
                GameObject expEffect = Instantiate(victoryEffect, transform.position, Quaternion.identity);
                Destroy(expEffect, 1f);
            }
        }
    }
}
