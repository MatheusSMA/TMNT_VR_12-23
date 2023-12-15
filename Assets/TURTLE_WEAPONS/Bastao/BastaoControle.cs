using UnityEngine;

public class BastaoControle : MonoBehaviour
{
    public Transform pontoFixacao1; // Atribua os objetos vazios para essas vari�veis no Inspector
    public Transform pontoFixacao2;
    public GameObject BastaoMesh;

    void Update()
    {
        // Atualize a posi��o do cilindro com base nos pontos de fixa��o
        transform.position = pontoFixacao1.position;

        // Atualize a escala do cilindro para estic�-lo entre os pontos de fixa��o
        if(Vector3.Distance(pontoFixacao1.position, pontoFixacao2.position) > 1)
        {
            transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, Vector3.Distance(pontoFixacao1.position, pontoFixacao2.position));
        }

        // Apontar o cilindro na dire��o do ponto fixado 2
        transform.LookAt(pontoFixacao2);
    }

    public void MakeVisible()
    {
        BastaoMesh.SetActive(true);
    }

    public void MakeInvisible()
    {
        BastaoMesh.SetActive(false);
    }
}
