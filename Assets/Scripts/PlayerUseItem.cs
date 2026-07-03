using QFSW.MOP2;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class PlayerUseItem : MonoBehaviour
{
    public string itemName;
    public enum ItemType
    {
        Grenade,
        Molotov,
        Dynamite,
        Gatolaid,
        Knife,
        Flashbang,
        Smoke
    }
    public enum SystemType
    {
        Default,
        ExplosionOnly,
        KnockbackOnly
    }
    public ObjectPool explosionPrefab;
    public float explosionHeight = 5;

    [SerializeField] private ItemType itemType;


    [Header("Grenade")]
    [SerializeField] private float fuseTime = 2f;
    [SerializeField] private float radius = 5f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Molotov Cocktail")]
    public ObjectPool Fireprefab;


    [Header("Gatolaid")]
    public float effectTime = 5f;
    [SerializeField] public float movementSpeedModifer;
    private PlayerMovement playerMovement;

    [Header("Knife Settings")]
    [SerializeField] private GameObject knifePrefab;
    [SerializeField] private Transform throwPoint;
    [SerializeField] private float knifeSpeed = 25f;
    [SerializeField] private float knifeDetectionRadius = 15f;
    [SerializeField] private float knifeFOV = 120f;


    [Header("System Type")]
    [SerializeField] private SystemType systemType;

    [Header("Explosion Damage")]
    [SerializeField] private float damage = 40f;

   
    public GameObject mesh;
    public ObjectPool pool;

    private void OnEnable()
    {
        if(itemType == ItemType.Grenade)
            StartCoroutine(FuseRoutine());

        if (itemType == ItemType.Dynamite)
            StartCoroutine(FuseRoutine());

        if (itemType == ItemType.Gatolaid)
        {
            playerMovement = FindObjectOfType<PlayerMovement>();
            StartCoroutine(UseGatolaid());
        }

        if (itemType == ItemType.Knife)
        {
            ThrowKnife();
            gameObject.SetActive(false);
        }

    }

    IEnumerator FuseRoutine()
    {
      
        yield return new WaitForSeconds(fuseTime);
        Detonate();
    }

    void Detonate()
    {
        GameObject explosion = explosionPrefab.GetObject(transform.position + new Vector3(0,explosionHeight,0), Quaternion.identity);
        explosion.GetComponent<ItemExplosion>().damage = damage;
        SFXManager.Instance.PlaySFX("PlayerItems/" + itemName + "Explosion",1f);
        //pool.Release(gameObject);
        gameObject.SetActive(false);

        
    }
   

    
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.layer == 17 && itemType == ItemType.Molotov)
        {
            Fireprefab.GetObject(transform.position, Quaternion.identity);
            SFXManager.Instance.PlaySFX("PlayerItems/" + itemName + "Explosion", 0.4f);
            gameObject.SetActive(false);
        }
        //if (collision.gameObject.layer == 17 && itemType == ItemType.Dynamite)
        //{
        //    Detonate();
        //}
    }
    public IEnumerator UseGatolaid()
    {
        playerMovement.itemUseMovementSpeed = movementSpeedModifer;

        yield return new WaitForSeconds(effectTime);

        playerMovement.itemUseMovementSpeed = 1;
        gameObject.SetActive(false);
    }


    Transform FindClosestEnemyInFOV()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, knifeDetectionRadius, enemyLayer);

        Transform bestTarget = null;
        float closestDist = Mathf.Infinity;

        foreach (var hit in hits)
        {
            Vector3 dir = (hit.transform.position - transform.position).normalized;

            float angle = Vector3.Angle(transform.forward, dir);

            if (angle <= knifeFOV * 0.5f)
            {
                float dist = Vector3.Distance(transform.position, hit.transform.position);

                if (dist < closestDist)
                {
                    closestDist = dist;
                    bestTarget = hit.transform;
                }
            }
        }

        return bestTarget;
    }

    void ThrowKnife()
    {
        Transform target = FindClosestEnemyInFOV();

        if (target == null)
        {
            Debug.Log("No target found");
            return;
        }

        GameObject knife = Instantiate(knifePrefab, throwPoint.position, Quaternion.identity);

        KnifeProjectile proj = knife.GetComponent<KnifeProjectile>();
        proj.Init(target, knifeSpeed);
    }
}