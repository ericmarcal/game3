using UnityEngine;
using System.Collections;
using Newtonsoft.Json.Linq;

[RequireComponent(typeof(BoxCollider2D), typeof(Animator), typeof(UniqueID))]
public class Tree : MonoBehaviour, ISavable
{
    public string ID => GetComponent<UniqueID>().ID;

    [Header("Vida da Árvore")]
    [SerializeField] private float treehealth = 3f;
    [SerializeField] private float maxHealth = 3f;
    public float CurrentHealth => treehealth;

    [Header("Regeneração")]
    [SerializeField] private float regrowDelay = 5f;

    [Header("Configurações do Drop")]
    [SerializeField] private GameObject woodWorldItemPrefab;
    [SerializeField] private ItemData woodItemData;
    [SerializeField] private float dropPopForce = 2.0f;
    [SerializeField] private float dropOffsetY = 0.5f;
    [SerializeField] private float dropRadius = 0.7f;
    [SerializeField] private int minWoodDrop = 1;
    [SerializeField] private int maxWoodDrop = 3;

    [Header("Efeitos Visuais")]
    [SerializeField] private ParticleSystem leafs;

    private Animator anim;
    private bool isCut;

    [System.Serializable]
    private struct TreeSaveData
    {
        public float health;
        public bool cut;
        public float[] position;
    }

    public object CaptureState()
    {
        return new TreeSaveData
        {
            health = this.treehealth,
            cut = this.isCut,
            position = new float[] { transform.position.x, transform.position.y, transform.position.z }
        };
    }

    public void RestoreState(object state)
    {
        var saveData = ((JObject)state).ToObject<TreeSaveData>();
        this.treehealth = saveData.health;
        this.isCut = saveData.cut;

        transform.position = new Vector3(saveData.position[0], saveData.position[1], saveData.position[2]);

        if (this.isCut)
        {
            anim.Play("Cut");
            GetComponent<Collider2D>().enabled = false;
            Invoke(nameof(RegrowTree), regrowDelay);
        }
    }

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    public void OnHit(GameObject hitter, int damageAmount = 1)
    {
        if (isCut || treehealth <= 0) return;
        treehealth -= damageAmount;
        anim.SetTrigger("isHit");
        if (leafs != null) leafs.Play();

        if (treehealth <= 0)
        {
            isCut = true;
            GetComponent<Collider2D>().enabled = false;
            anim.SetTrigger("cut");
            int amount = Random.Range(minWoodDrop, maxWoodDrop + 1);
            if (hitter.TryGetComponent<ItemContainer>(out var itemContainer) && hitter.CompareTag("NPC"))
            {
                itemContainer.AddItem(woodItemData, amount);
            }
            else
            {
                for (int i = 0; i < amount; i++)
                {
                    DropWood();
                }
            }
            Invoke(nameof(RegrowTree), regrowDelay);
        }
    }

    private void DropWood()
    {
        if (woodWorldItemPrefab == null || woodItemData == null) return;
        Vector2 randomOffset = Random.insideUnitCircle * dropRadius;
        Vector3 spawnPosition = transform.position + new Vector3(randomOffset.x, randomOffset.y - dropOffsetY, 0f);
        GameObject woodInstance = Instantiate(woodWorldItemPrefab, spawnPosition, Quaternion.identity);
        WorldItem worldItemScript = woodInstance.GetComponent<WorldItem>();
        if (worldItemScript != null)
        {
            worldItemScript.itemData = woodItemData;
            worldItemScript.quantity = 1;
            Vector2 forceDirection = new Vector2(Random.Range(-0.5f, 0.5f), 1f);
            worldItemScript.SetupSpawnedItemParameters(spawnPosition, forceDirection, dropPopForce);
        }
    }

    private void RegrowTree()
    {
        isCut = false;
        treehealth = maxHealth;
        GetComponent<Collider2D>().enabled = true;
        anim.SetTrigger("regrow");
    }

    // << BLOCO DE REGISTO ADICIONADO >>
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
}