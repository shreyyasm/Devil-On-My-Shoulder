using QFSW.MOP2;
using UnityEngine;

[CreateAssetMenu(menuName = "Devil On My Shoulder/Enemy Progression Config")]
public class EnemyProgressionConfig : ScriptableObject
{
    [Header("Enemy Pools")]
    [SerializeField] private ObjectPool jocky;
    [SerializeField] private ObjectPool jockyGun;
    [SerializeField] private ObjectPool spider;
    [SerializeField] private ObjectPool janitor;
    [SerializeField] private ObjectPool demonicBook;
    [SerializeField] private ObjectPool messLady;

    public ObjectPool GetRandomPool(int setIndex)
    {
        int availablePoolCount = setIndex switch
        {
            >= 8 => 6,
            >= 6 => 5,
            >= 4 => 4,
            >= 2 => 3,
            _ => 2
        };

        ObjectPool[] pools =
        {
            jocky,
            jockyGun,
            spider,
            janitor,
            demonicBook,
            messLady
        };

        for (int attempt = 0; attempt < availablePoolCount; attempt++)
        {
            ObjectPool pool = pools[Random.Range(0, availablePoolCount)];

            if (pool != null)
                return pool;
        }

        return null;
    }
}
