using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public enum Moving { Move, None }
public class PlayerController : MonoBehaviour, ISavable
{
    [SerializeField] new string name;
    [SerializeField] Sprite sprite;
    [SerializeField] Sprite spriteGirl;
    [SerializeField] Moving isHow;

    bool howIs = true; //True si es hombre y False si es mujer


    private Vector2 input;//Input para mover al personaje

    public static PlayerController i { get; private set; }
    private Character character;

    private void Awake() // Genera la animaciones al caminar del personaje
    {
        i = this;
        character = GetComponent<Character>();
    }

    private void Update()
    {
        if (isHow == Moving.None)
            ChangeSprites();
    }

    public void HandleUpdate()// Genera el moviento de forma horizontal y vertical del personaje
    {
        if (!character.IsMoving)
        {
            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");

            if (input.x != 0) input.y = 0;

            if (input != Vector2.zero)
            {
                StartCoroutine(character.Move(input, OnMoveOver));
            }
        }

        character.HandleUpdate();

        if (Input.GetKeyDown(KeyCode.Z))
            StartCoroutine(Interact());
    }

    IEnumerator Interact()//Interactuar con un NPC
    {
        var facingDir = new Vector3(character.Animator.MoveX, character.Animator.MoveY);
        var interactPos = transform.position + facingDir;

        //Debug.DrawLine(transform.position, interactPos, Color.green, 0.5f);

        var collider = Physics2D.OverlapCircle(interactPos, 0.3f, GameLayers.i.InteractableLayer | GameLayers.i.WaterLayer);
        if (collider != null)
        {
            yield return collider.GetComponent<Interactable>()?.Interact(transform);
        }
    }

    IPlayerTriggerable currentlyInTrigger;

    public void OnMoveOver() //Permite comprobar si se encuentra en la hierba, a la vista de un entrenador o encima de un story item y hasta que no cumpla los requisitos no lo deja pasar
    {
        var colliders = Physics2D.OverlapCircleAll(transform.position - new Vector3(0, character.OffsetY), 0.2f, GameLayers.i.TriggerLayers);

        IPlayerTriggerable triggerable = null;
        foreach (var collider in colliders)
        {
            triggerable = collider.GetComponent<IPlayerTriggerable>();
            if (triggerable != null)
            {
                if (triggerable == currentlyInTrigger && !triggerable.TriggerRepeatedly)
                    //if (triggerable == currentlyInTrigger)
                    break;

                triggerable.OnPlayerTrigger(this); //Dispara un trigger que genera los encuentros con los pokemones y entrenadores
                currentlyInTrigger = triggerable;
                break;
            }
        }
        if (colliders.Count() == 0 || triggerable != currentlyInTrigger)
            currentlyInTrigger = null;

    }

    public object CaptureState() //Guardar estado
    {
        var saveData = new PlayerSaveData()
        {
            name = this.name,
            position = new float[] { transform.position.x, transform.position.y },
            pokemons = GetComponent<PokemonParty>().Pokemons.Select(p => p.GetSaveData()).ToList(),
            isHow = this.howIs ? Moving.Move : Moving.None,
        };
        return saveData;
    }

    public void RestoreState(object state) //Cargar estado
    {
        var saveData = (PlayerSaveData)state;
        name = saveData.name;
        var pos = saveData.position;
        transform.position = new Vector3(pos[0], pos[1]);
        isHow = saveData.isHow;
        if (isHow != Moving.None)
        {
            howIs = true;
        }

        ChangeSprites();

        //Cargar party
        GetComponent<PokemonParty>().Pokemons = saveData.pokemons.Select(s => new Pokemon(s)).ToList();
    }
    public void ChangeSprites()
    {
        if (isHow == Moving.None)
        {
            howIs = false;
            character.Animator.ChangeSprites(Moving.None);
            isHow = Moving.Move;
        }
        else
        {
            character.Animator.ChangeSprites(Moving.Move);
        }
    }
    public string Name
    {
        get => name;
        set => name = value;
    }

    public Sprite Sprite
    {
        get => sprite;
    }

    public Character Character => character;

    public Moving IsHow
    {
        get => isHow;
        set => isHow = value;
    }
}

[Serializable]
public class PlayerSaveData
{
    public string name;
    public float[] position;
    public Moving isHow;
    public List<PokemonSaveData> pokemons;
}