using UnityEngine;

public class ChuyểnSence : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            //go to next level
            senceController.instance.NextLevel();
            
        }
    }

}
