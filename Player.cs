using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Linq;

public class Player : MonoBehaviour, IDamageable, ISavable
{
    public string ID { get => "Player"; }
    public static Player instance;

    #region Variáveis
    [Header("Velocidade")]
    public float speed;
    public float runSpeed;
    [Header("Atributos do Jogador")]
    public float maxHealth = 10f;
    public float currentHealth;
    public float maxStamina = 100f;
    public float currentStamina;
    [Header("Configuração de Morte")]
    [SerializeField] private float deathSequenceDelay = 2f;
    [Header("Custos de Vigor (Stamina)")]
    [SerializeField] private float runStaminaCostPerSecond = 10f;
    [SerializeField] private float rollStaminaCost = 25f;
    [SerializeField] private float staminaRegenRate = 15f;
    [Header("Efeitos de Status")]
    [Range(0.1f, 1f)]
    [SerializeField] private float slowEffectMultiplier = 0.5f;
    [Header("Teclas de Ação")]
    [SerializeField] private KeyCode useItemKey = KeyCode.F;
    [SerializeField] private KeyCode secondaryActionKey = KeyCode.E;
    [Header("Combate e Dano")]
    [SerializeField] private float invulnerabilityDuration = 1.5f;
    public Transform playerActionPoint;
    [SerializeField] private float actionDistance = 0.8f;
    [SerializeField] private float playerActionRadius = 0.5f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private LayerMask resourceLayer;
    [SerializeField] private float playerAttackDamage = 1f;
    [SerializeField] private float comboChainWindowDuration = 0.5f;
    [Header("Rolagem")]
    [SerializeField] private float rollSpeed = 8f;

    [Header("Referências do Mundo")]
    public Grid grid;
    [Tooltip("Arraste para aqui o GameObject da sua câmara principal que tem o script CameraFollow.")]
    [SerializeField] private CameraFollow mainCameraFollow;

    [Header("Posicionamento e Construção")]
    [SerializeField] private LayerMask placementObstacleLayerMask;
    [Tooltip("Adicione aqui todos os Tilemaps que devem BLOQUEAR a construção (água, caminhos, etc.).")]
    [SerializeField] private List<Tilemap> blockingTilemaps;
    [SerializeField] private float placementDistance = 1f;
    [SerializeField] private GameObject cornerMarkerPrefab;
    [Tooltip("O novo valor 'Size' da câmara no modo construção (ex: 15 para afastar). O valor padrão da sua câmara é 8.")]
    [SerializeField] private float buildingModeZoom = 15f;
    [Tooltip("A duração em segundos do efeito de zoom.")]
    [SerializeField] private float zoomDuration = 0.5f;

    private bool _isDead = false;
    private bool _isRunning, _isRolling, _isCutting, _isDigging, _isWatering, _isMining, _isAttacking = false, isInvulnerable = false;
    public bool isFishing, isPaused, canCatchFish = false;
    private Rigidbody2D rig;
    private PlayerAnim playerAnimationController;
    private PlayerFishing playerFishing;
    private SpriteRenderer spriteRenderer;
    private Color originalSpriteColor;
    private Vector2 _direction;
    public Vector2 lastMoveDirection = Vector2.down;
    public ToolType currentTool = ToolType.None;
    [HideInInspector] public int currentHotbarIndex = 0;
    private Vector2 rollDirection;
    private float initialSpeed;
    private int attackComboCount = 0;
    private bool canChainToNextCombo = false;
    private float lastChainInputTime = 0f;
    private int activeSlowsCount = 0;
    private float speedMultiplier = 1f;
    private Coroutine damageFlashCoroutine;
    private Coroutine invulnerabilityCoroutine;

    private bool isInItemPlacementMode = false;
    private GameObject currentItemGhost;
    private PlacementGhost itemGhostScript;
    private ItemData currentPlaceableItem;
    private bool canPlaceItem = false;

    private bool isInBuildingPlacementMode = false;
    private ItemData currentBuildingKit;
    private bool canPlaceBuilding = false;
    private readonly List<PlacementMarker> cornerMarkers = new List<PlacementMarker>();
    private Vector3Int currentPlacementAnchor;
    #endregion

    #region Properties
    public Vector2 direction => _direction;
    public bool isRunning => _isRunning;
    public bool isRolling => _isRolling;
    public bool isAttacking => _isAttacking;
    public bool IsBusy() => _isRolling || isFishing || _isCutting || _isDigging || _isWatering || _isMining || _isAttacking;
    public bool IsDead() => _isDead;
    #endregion

    #region Unity Callbacks
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        rig = GetComponent<Rigidbody2D>();
        playerAnimationController = GetComponent<PlayerAnim>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerFishing = GetComponent<PlayerFishing>();
        currentHealth = maxHealth;
        currentStamina = maxStamina;
    }
    private void Start()
    {
        initialSpeed = speed;
        if (spriteRenderer != null) originalSpriteColor = spriteRenderer.color;
        if (HotbarController.instance != null) { HotbarController.instance.InitializeHotbar(); }
        UseHotbarItem(currentHotbarIndex);
    }
    private void Update()
    {
        if (IsDead()) return;

        HandleInput();
        HandleHotbarInput();

        if (isInItemPlacementMode) { HandleItemPlacementMode(); }
        else if (isInBuildingPlacementMode) { HandleBuildingPlacementMode(); }

        if (Input.GetKeyDown(KeyCode.Tab)) { if (ShopUIManager.instance != null && ShopUIManager.instance.IsShopOpen) { ShopUIManager.instance.CloseShop(); } else if (ChestUIManager.instance != null && ChestUIManager.instance.currentContainer != null) { ChestUIManager.instance.CloseChestUI(); } else if (InventoryManager.instance != null) { InventoryManager.instance.ToggleInventory(!InventoryManager.instance.IsInventoryOpen()); } }

        if (isPaused || IsBusy()) { if (_isAttacking && canChainToNextCombo && Input.GetMouseButtonDown(0)) HandleAttackInput(); if (_isAttacking && canChainToNextCombo && (Time.time - lastChainInputTime > comboChainWindowDuration)) FinishAttackSequence(); else if (_isAttacking && !canChainToNextCombo && attackComboCount > 0 && (Time.time - lastChainInputTime > comboChainWindowDuration + 0.5f)) FinishAttackSequence(); return; }

        HandleRun();
        HandleStaminaRegen();
        HandleActionInputs();

        if (Input.GetKeyDown(useItemKey)) { PlayerItens.instance.ConsumeFirstAvailableHotbarItem(); }
    }
    private void FixedUpdate() { if (IsDead()) { rig.velocity = Vector2.zero; return; } if (IsBusy() || isInBuildingPlacementMode || isInItemPlacementMode) { if (_isRolling) { rig.velocity = rollDirection.normalized * rollSpeed; } else { rig.velocity = Vector2.zero; } } else { HandleMovement(); } }
    #endregion

    #region Save/Load
    [System.Serializable]
    private struct PlayerSaveData { public float currentHealth; public float currentStamina; public float[] position; }
    public object CaptureState() { return new PlayerSaveData { currentHealth = this.currentHealth, currentStamina = this.currentStamina, position = new float[] { transform.position.x, transform.position.y, transform.position.z } }; }
    public void RestoreState(object state)
    {
        var saveData = ((JObject)state).ToObject<PlayerSaveData>();
        this.currentHealth = saveData.currentHealth;
        this.currentStamina = saveData.currentStamina;
        TeleportTo(new Vector3(saveData.position[0], saveData.position[1], saveData.position[2]));
    }
    #endregion

    #region Sistema de Transição de Salas

    // << NOVA ROTINA CENTRALIZADA >>
    public IEnumerator PerformRoomTransition(Vector3 spawnPosition, Bounds newCameraBounds, Vector3 cameraTargetPosition, float panDuration)
    {
        isPaused = true;
        if (rig != null) rig.velocity = Vector2.zero;

        if (mainCameraFollow != null)
        {
            mainCameraFollow.PanToPosition(cameraTargetPosition, panDuration);
        }

        yield return new WaitForSeconds(panDuration);

        TeleportTo(spawnPosition);

        if (mainCameraFollow != null)
        {
            mainCameraFollow.SetBounds(newCameraBounds);
            mainCameraFollow.SnapToTarget(); // Garante que a câmara se ajusta instantaneamente
        }

        isPaused = false;
    }

    #endregion

    #region Sistema de Posicionamento e Construção

    private void UseHotbarItem(int slotIndex)
    {
        currentHotbarIndex = slotIndex;
        if (PlayerItens.instance == null) return;
        InventorySlot slot = PlayerItens.instance.GetSlot(ContainerType.Hotbar, slotIndex);
        if (HotbarController.instance != null) HotbarController.instance.UpdateSelection(slotIndex);

        if (slot == null || slot.item == null)
        {
            currentTool = ToolType.None;
            ExitItemPlacementMode();
            ExitBuildingPlacementMode();
            return;
        }

        switch (slot.item.itemType)
        {
            case ItemType.Placeable:
                ExitBuildingPlacementMode();
                EnterItemPlacementMode(slot.item);
                currentTool = ToolType.None;
                break;
            case ItemType.ConstructionKit:
                ExitItemPlacementMode();
                EnterBuildingPlacementMode(slot.item);
                currentTool = ToolType.None;
                break;
            default:
                ExitItemPlacementMode();
                ExitBuildingPlacementMode();
                currentTool = (slot.item.itemType == ItemType.Ferramenta) ? slot.item.associatedTool : ToolType.None;
                break;
        }
    }

    private void EnterItemPlacementMode(ItemData itemToPlace)
    {
        ExitItemPlacementMode();
        isInItemPlacementMode = true;
        currentPlaceableItem = itemToPlace;
        currentItemGhost = Instantiate(itemToPlace.placeablePrefab);
        itemGhostScript = currentItemGhost.AddComponent<PlacementGhost>();
        foreach (var collider in currentItemGhost.GetComponents<Collider2D>()) { collider.enabled = false; }
        foreach (var monoBehaviour in currentItemGhost.GetComponents<MonoBehaviour>()) { if (monoBehaviour != itemGhostScript) { monoBehaviour.enabled = false; } }
    }

    private void HandleItemPlacementMode()
    {
        if (currentItemGhost == null) return;
        Vector3 placementPoint = transform.position + ((Vector3)lastMoveDirection.normalized * placementDistance);
        Vector3Int gridPosition = grid.WorldToCell(placementPoint);
        Vector3 ghostPosition = grid.GetCellCenterWorld(gridPosition);
        currentItemGhost.transform.position = ghostPosition;
        canPlaceItem = !Physics2D.OverlapCircle(ghostPosition, 0.1f, placementObstacleLayerMask);
        itemGhostScript.SetValidity(canPlaceItem);
    }

    private void PlaceItem(Vector3 position)
    {
        Instantiate(currentPlaceableItem.placeablePrefab, position, Quaternion.identity);
        PlayerItens.instance.RemoveQuantityFromSlot(ContainerType.Hotbar, currentHotbarIndex, 1);
        InventoryManager.instance.UpdateAllVisuals();
        InventorySlot currentSlot = PlayerItens.instance.GetSlot(ContainerType.Hotbar, currentHotbarIndex);
        if (currentSlot == null || currentSlot.item == null || currentSlot.quantity == 0)
        {
            ExitItemPlacementMode();
        }
    }

    private void ExitItemPlacementMode()
    {
        if (isInItemPlacementMode)
        {
            isInItemPlacementMode = false;
            if (currentItemGhost != null) { Destroy(currentItemGhost); }
            currentItemGhost = null;
            currentPlaceableItem = null;
            itemGhostScript = null;
        }
    }

    private void EnterBuildingPlacementMode(ItemData kit)
    {
        if (kit == null || kit.buildingData == null || cornerMarkerPrefab == null)
        {
            Debug.LogError("O Kit de Construção ou o Prefab do Marcador não estão configurados!");
            return;
        }

        ExitBuildingPlacementMode();
        isInBuildingPlacementMode = true;
        currentBuildingKit = kit;

        if (mainCameraFollow != null)
        {
            mainCameraFollow.SetZoom(buildingModeZoom, zoomDuration);
        }

        for (int i = 0; i < 4; i++)
        {
            GameObject markerObj = Instantiate(cornerMarkerPrefab);
            cornerMarkers.Add(markerObj.GetComponent<PlacementMarker>());
        }
    }

    private void HandleBuildingPlacementMode()
    {
        if (cornerMarkers.Count != 4) return;

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int gridPosition = grid.WorldToCell(mouseWorldPos);

        float halfWidth = currentBuildingKit.buildingData.size.x / 2.0f;
        float halfHeight = currentBuildingKit.buildingData.size.y / 2.0f;
        Vector3 centerPoint = grid.GetCellCenterWorld(gridPosition) + new Vector3(halfWidth - 0.5f, halfHeight - 0.5f, 0);

        cornerMarkers[0].transform.position = centerPoint + new Vector3(-halfWidth, halfHeight, 0);
        cornerMarkers[1].transform.position = centerPoint + new Vector3(halfWidth, halfHeight, 0);
        cornerMarkers[2].transform.position = centerPoint + new Vector3(-halfWidth, -halfHeight, 0);
        cornerMarkers[3].transform.position = centerPoint + new Vector3(halfWidth, -halfHeight, 0);

        currentPlacementAnchor = grid.WorldToCell(cornerMarkers[2].transform.position);

        canPlaceBuilding = IsAreaFree(currentPlacementAnchor, currentBuildingKit.buildingData.size);

        Color colorToApply = canPlaceBuilding ? Color.green : Color.red;
        foreach (var marker in cornerMarkers)
        {
            if (marker != null) marker.SetColor(colorToApply);
        }
    }

    private bool IsAreaFree(Vector3Int originCell, Vector2Int size)
    {
        Bounds buildingBounds = new Bounds(grid.GetCellCenterWorld(originCell) + new Vector3(size.x / 2.0f - 0.5f, size.y / 2.0f - 0.5f, 0), new Vector3(size.x, size.y, 1));
        if (Physics2D.OverlapBox(buildingBounds.center, buildingBounds.size, 0, placementObstacleLayerMask))
        {
            return false;
        }

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector3Int tileToCheck = new Vector3Int(originCell.x + x, originCell.y + y, originCell.z);

                if (blockingTilemaps != null)
                {
                    foreach (var blockerMap in blockingTilemaps)
                    {
                        if (blockerMap != null && blockerMap.HasTile(tileToCheck))
                        {
                            return false;
                        }
                    }
                }
            }
        }

        return true;
    }

    private void PlaceBuilding(Vector3Int anchorGridPosition)
    {
        ConstructionManager.instance.Build(currentBuildingKit.buildingData, anchorGridPosition);
        PlayerItens.instance.RemoveQuantityFromSlot(ContainerType.Hotbar, currentHotbarIndex, 1);
        ExitBuildingPlacementMode();
        UseHotbarItem(currentHotbarIndex);
    }

    private void ExitBuildingPlacementMode()
    {
        if (isInBuildingPlacementMode)
        {
            isInBuildingPlacementMode = false;

            if (mainCameraFollow != null)
            {
                mainCameraFollow.ResetZoom(zoomDuration);
            }

            foreach (var marker in cornerMarkers)
            {
                if (marker != null) Destroy(marker.gameObject);
            }
            cornerMarkers.Clear();
            currentBuildingKit = null;
        }
    }

    #endregion

    #region Ações e Comportamentos

    private void HandleInput() { _direction = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")); if (_direction.sqrMagnitude > 0.01f) { lastMoveDirection = _direction.normalized; } }
    private void HandleMovement() { if (rig != null) rig.MovePosition(rig.position + _direction.normalized * (speed * speedMultiplier) * Time.fixedDeltaTime); }
    private void HandleActionInputs()
    {
        playerActionPoint.position = (Vector2)transform.position + lastMoveDirection * actionDistance;

        if (Input.GetMouseButtonDown(1)) { TryStartRolling(); }

        if (Input.GetKeyDown(secondaryActionKey))
        {
            if (isInItemPlacementMode && canPlaceItem)
            {
                PlaceItem(currentItemGhost.transform.position);
                return;
            }

            if (isInBuildingPlacementMode && canPlaceBuilding)
            {
                PlaceBuilding(currentPlacementAnchor);
                return;
            }

            Collider2D[] hitObjects = Physics2D.OverlapCircleAll(playerActionPoint.position, playerActionRadius, interactableLayer);
            foreach (var hit in hitObjects) { if (hit.TryGetComponent<Harvestable>(out var harvestable)) { if (FarmingManager.instance != null) { FarmingManager.instance.Harvest(harvestable.crop.gridPosition); } return; } if (hit.TryGetComponent<ShopInteraction>(out var shopInteraction)) { shopInteraction.ToggleShop(); return; } if (hit.TryGetComponent<ChestInteraction>(out var chestInteraction)) { chestInteraction.ToggleChest(); return; } if (hit.TryGetComponent<NPCController>(out var npc) && hit.TryGetComponent<ItemContainer>(out var container)) { if (ChestUIManager.instance != null) { ChestUIManager.instance.OpenChestUI(container); return; } } }
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (currentTool == ToolType.Sword) { HandleAttackInput(); }
            else if (currentTool != ToolType.None) { HandleToolAction(); }
        }
    }

    private void HandleToolAction()
    {
        playerActionPoint.position = (Vector2)transform.position + lastMoveDirection * actionDistance;
        isPaused = true;
        switch (currentTool)
        {
            case ToolType.Axe: _isCutting = true; break;
            case ToolType.Shovel: _isDigging = true; break;
            case ToolType.WateringCan: _isWatering = true; break;
            case ToolType.Pickaxe: _isMining = true; break;
            case ToolType.FishingRod: if (playerFishing != null) playerFishing.StartFishingAction(); break;
        }
        if (playerAnimationController != null)
        {
            if (currentTool == ToolType.Pickaxe) playerAnimationController.TriggerMineAnimation();
            else if (currentTool != ToolType.FishingRod) playerAnimationController.TriggerToolAnimation(currentTool);
        }
        else { OnActionFinished(); }
    }

    public void PerformToolActionCheck()
    {
        if (playerActionPoint == null) return;
        Vector3 actionPosition = playerActionPoint.position;

        Collider2D[] hitResources = Physics2D.OverlapCircleAll(actionPosition, playerActionRadius, resourceLayer);
        if (hitResources.Length > 0)
        {
            foreach (Collider2D hit in hitResources)
            {
                if (hit.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(1);
                    return;
                }
            }
        }

        if (currentTool == ToolType.Shovel || currentTool == ToolType.WateringCan)
        {
            if (grid != null)
            {
                Vector3Int gridPosition = grid.WorldToCell(actionPosition);
                if (currentTool == ToolType.Shovel) FarmingManager.instance.Dig(gridPosition);
                else if (currentTool == ToolType.WateringCan) FarmingManager.instance.Water(gridPosition);
            }
            return;
        }

        if (currentTool == ToolType.FishingRod) { if (playerFishing != null) playerFishing.SpawnFishIfCaught(); }
    }

    public void OpenNextComboWindow() { if (_isAttacking && attackComboCount < 3) { canChainToNextCombo = true; lastChainInputTime = Time.time; isPaused = false; } else if (_isAttacking && attackComboCount >= 3) { FinishAttackSequence(); } }
    public void TakeDamage(float damageAmount) { if (isInvulnerable || IsDead()) return; currentHealth -= damageAmount; TakeDamageFeedback(); if (currentHealth <= 0) { currentHealth = 0; Die(); } }
    private void Die() { _isDead = true; isPaused = true; playerAnimationController.TriggerDeathAnimation(); GetComponent<Collider2D>().enabled = false; rig.velocity = Vector2.zero; rig.isKinematic = true; Invoke(nameof(ShowGameOverScreen), deathSequenceDelay); }
    private void ShowGameOverScreen() { UIManager uiManager = FindObjectOfType<UIManager>(); if (uiManager != null) { uiManager.ShowGameOverScreen(); } }
    private void HandleStaminaRegen() { if (!_isRunning && !_isRolling) { if (currentStamina < maxStamina) { currentStamina += staminaRegenRate * Time.deltaTime; currentStamina = Mathf.Min(currentStamina, maxStamina); } } }
    public void ApplySlow() { activeSlowsCount++; if (activeSlowsCount == 1) speedMultiplier = slowEffectMultiplier; }
    public void RemoveSlow() { activeSlowsCount--; if (activeSlowsCount <= 0) { activeSlowsCount = 0; speedMultiplier = 1f; } }
    public void RestoreHealth(float amount) { if (amount <= 0 || IsDead()) return; currentHealth += amount; currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth); }
    public void RestoreStamina(float amount) { if (amount <= 0) return; currentStamina += amount; currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina); }
    private void HandleRun() { bool wantsToRun = Input.GetKey(KeyCode.LeftShift); if (wantsToRun && _direction.sqrMagnitude > 0 && currentStamina > 0) { speed = runSpeed; _isRunning = true; currentStamina -= runStaminaCostPerSecond * Time.deltaTime; } else { speed = initialSpeed; _isRunning = false; } }
    private void TryStartRolling() { if (currentStamina >= rollStaminaCost) { currentStamina -= rollStaminaCost; rollDirection = (_direction.sqrMagnitude > 0.01f) ? _direction : lastMoveDirection; if (rollDirection.sqrMagnitude > 0.01f) { ExitItemPlacementMode(); ExitBuildingPlacementMode(); isInvulnerable = true; isPaused = true; _isRolling = true; playerAnimationController.TriggerRollAnimation(rollDirection); } } }
    private void HandleHotbarInput() { if (PlayerItens.instance == null) return; float scroll = Input.GetAxis("Mouse ScrollWheel"); if (scroll != 0f) { currentHotbarIndex += scroll < 0f ? 1 : -1; if (currentHotbarIndex >= PlayerItens.instance.hotbarSize) currentHotbarIndex = 0; if (currentHotbarIndex < 0) currentHotbarIndex = PlayerItens.instance.hotbarSize - 1; UseHotbarItem(currentHotbarIndex); return; } for (int i = 0; i < PlayerItens.instance.hotbarSize; i++) { if (Input.GetKeyDown(KeyCode.Alpha1 + i)) { currentHotbarIndex = i; UseHotbarItem(currentHotbarIndex); return; } } }
    private void OnActionFinished() { isPaused = false; _isRolling = false; _isCutting = false; _isDigging = false; _isWatering = false; _isMining = false; isFishing = false; if (rig != null) rig.velocity = Vector2.zero; if (_isAttacking) { _isAttacking = false; attackComboCount = 0; canChainToNextCombo = false; if (playerAnimationController != null) playerAnimationController.ResetAttackAnimationParams(); } }
    private void HandleAttackInput() { if (!_isAttacking) { isPaused = true; _isAttacking = true; attackComboCount = 1; canChainToNextCombo = false; playerAnimationController.TriggerAttackAnimation(attackComboCount); lastChainInputTime = Time.time; } else if (canChainToNextCombo && attackComboCount < 3) { isPaused = true; attackComboCount++; canChainToNextCombo = false; playerAnimationController.TriggerAttackAnimation(attackComboCount); lastChainInputTime = Time.time; } }
    public void PerformSwordAttack() { if (playerActionPoint == null) return; Collider2D[] hitObjects = Physics2D.OverlapCircleAll(playerActionPoint.position, playerActionRadius, interactableLayer); foreach (Collider2D hit in hitObjects) { hit.GetComponent<IDamageable>()?.TakeDamage(playerAttackDamage); } }
    public void FinishRolling() { isInvulnerable = false; OnActionFinished(); }
    public void FinishCurrentAction() => OnActionFinished();
    public void FinishAttackSequence() => OnActionFinished();
    public void TakeDamageFeedback() { if (isInvulnerable) return; isInvulnerable = true; playerAnimationController.TriggerTakeHitAnimation(); if (damageFlashCoroutine != null) StopCoroutine(damageFlashCoroutine); damageFlashCoroutine = StartCoroutine(FlashRedCoroutine()); if (invulnerabilityCoroutine != null) StopCoroutine(invulnerabilityCoroutine); invulnerabilityCoroutine = StartCoroutine(InvulnerabilityWindowCoroutine()); }
    private IEnumerator FlashRedCoroutine() { spriteRenderer.color = Color.red; yield return new WaitForSeconds(0.1f); spriteRenderer.color = originalSpriteColor; }
    private IEnumerator InvulnerabilityWindowCoroutine() { yield return new WaitForSeconds(invulnerabilityDuration); isInvulnerable = false; }
    private void OnDrawGizmosSelected() { if (playerActionPoint != null) { Gizmos.color = Color.blue; Gizmos.DrawWireSphere(playerActionPoint.position, playerActionRadius); } }
    public void TeleportTo(Vector3 newPosition) { gameObject.SetActive(false); transform.position = newPosition; if (rig != null) { rig.velocity = Vector2.zero; } gameObject.SetActive(true); }

    protected virtual void OnEnable()
    {
        if (SaveLoadManager.instance != null)
        {
            SaveLoadManager.instance.RegisterSavable(this);
        }
    }

    protected virtual void OnDisable()
    {
        if (SaveLoadManager.instance != null)
        {
            SaveLoadManager.instance.UnregisterSavable(this);
        }
    }
    #endregion
}