using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private Animator animator;
    [SerializeField]
    private Rigidbody2D rb;
    public float speed = 5f;

    // Direção atual do jogador (pública para outros scripts acessarem)
    public Vector2 lastDirection { get; private set; } = Vector2.down;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        // Previne movimento diagonal - prioriza o eixo com maior input
        if (Mathf.Abs(moveHorizontal) > 0 && Mathf.Abs(moveVertical) > 0)
        {
            if (Mathf.Abs(moveHorizontal) >= Mathf.Abs(moveVertical))
            {
                moveVertical = 0; // Prioriza movimento horizontal
            }
            else
            {
                moveHorizontal = 0; // Prioriza movimento vertical
            }
        }

        Vector2 movement = new Vector2(moveHorizontal, moveVertical);
        rb.MovePosition(rb.position + movement.normalized * Time.fixedDeltaTime * speed);

        // Controle de animações baseado na direção do movimento
        // Movimento horizontal
        if (moveHorizontal < 0) // Movendo para a esquerda
        {
            this.animator.SetBool("walking_left", true);
            this.animator.SetBool("walking_right", false);
            this.animator.SetBool("walking_up", false);
            this.animator.SetBool("walking_down", false);
            lastDirection = Vector2.left;
        }
        else if (moveHorizontal > 0) // Movendo para a direita
        {
            this.animator.SetBool("walking_left", false);
            this.animator.SetBool("walking_right", true);
            this.animator.SetBool("walking_up", false);
            this.animator.SetBool("walking_down", false);
            lastDirection = Vector2.right;
        }
        else // Sem movimento horizontal
        {
            this.animator.SetBool("walking_left", false);
            this.animator.SetBool("walking_right", false);
            if (moveVertical == 0)
            {
                this.animator.SetBool("not_walking", true);
            }
        }

        // Movimento vertical
        if (moveVertical > 0) // Movendo para cima
        {
            this.animator.SetBool("walking_up", true);
            this.animator.SetBool("walking_down", false);
            this.animator.SetBool("walking_left", false);
            this.animator.SetBool("walking_right", false);
            lastDirection = Vector2.up;
        }
        else if (moveVertical < 0) // Movendo para baixo
        {
            this.animator.SetBool("walking_up", false);
            this.animator.SetBool("walking_down", true);
            this.animator.SetBool("walking_left", false);
            this.animator.SetBool("walking_right", false);
            lastDirection = Vector2.down;
        }
        else // Sem movimento vertical
        {
            this.animator.SetBool("walking_up", false);
            this.animator.SetBool("walking_down", false);
            if (moveHorizontal == 0)
            {
                this.animator.SetBool("not_walking", true);
            }
        }

        // Verifica se está parado (sem movimento horizontal nem vertical)
        if (moveHorizontal == 0 && moveVertical == 0)
        {
            this.animator.SetBool("not_walking", true);
        }
        else
        {
            this.animator.SetBool("not_walking", false);
        }
    }
}
