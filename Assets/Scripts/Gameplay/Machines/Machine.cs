﻿using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// All machine logic is contained within here.
/// </summary>
public class Machine : Draggable {
    [SerializeField] public RecipeScriptableObj recipeObj;

    [NonSerialized] public List<OutputPort> OutputPorts = new List<OutputPort>();
    [NonSerialized] public List<InputPort> InputPorts = new List<InputPort>();
    private int _ticksSinceProduced;
    private bool _pokedThisTick;

    /// <summary>
    /// A list of resources that this machine just produced this tick.
    /// Currently, it just includes all resources stored in this machine.
    /// </summary>
    public List<Resource> OutputBuffer { get; private set; }
    public List<Resource> storage { get; private set; }

    private Vector2 _dragDirection;
    private List<MachineBluePrint> _dragBluePrints;
    
    [SerializeField] private bool _shouldPrint;
    [SerializeField] private bool _shouldBreak;
    
    protected void Awake() {
        OutputBuffer = new List<Resource>();
        storage = new List<Resource>();
    }
    
    void Start() {
        _dragBluePrints = new List<MachineBluePrint>();
    }


    /// <summary>
    /// Performs <paramref name="func"/> on all machines in <typeparamref name="portlist"/>.
    /// </summary>
    /// <param name="portList">The list of ports to iterate over</param>
    /// <param name="func">The function to perform on each machine in <paramref name="portList"/></param>
    private static void foreachMachine(List<MachinePort> portList, Action<Machine> func) {
        foreach (MachinePort i in portList) {
            var inputMachine = i.ConnectedMachine;
            if (inputMachine) {
                func(inputMachine);
            }
        }
    }
    
    /// <summary>
    /// Returns true if the connected input machines have enough resources for this machine to produce an output.
    /// </summary>
    /// <returns></returns>
    protected bool _checkEnoughInput() {
        var actualInputs = new List<Resource>();
        foreachMachine(new List<MachinePort>(InputPorts), m => actualInputs.AddRange(m.OutputBuffer));
        
        bool ret = recipeObj.CheckInputs(actualInputs);
        if (_shouldPrint) {
            // print("Input resources: ");
            foreach (Resource i in actualInputs) { print(i);}
            print("Enough input: "  +ret);
        }

        return ret;
    }
    
    /// <summary>
    /// Removes all resources from OutputBuffer.
    /// </summary>
    public void ClearOutput() {
        OutputBuffer.Clear();
    }
    
    /// <summary>
    /// Moves <paramref name="r"/> to this machine's position.
    /// </summary>
    /// <param name="r">The resource to move</param>
    /// <param name="destroyOnComplete">Whether <paramref name="r"/> should self-destruct after moving here</param>
    public void MoveHere(Resource r, bool destroyOnComplete) {
        var position = transform.position;
        var instantiatePos = new Vector3(position.x, position.y, r.gameObject.transform.position.z);
        r.MySmoothSprite.Move(instantiatePos, destroyOnComplete);
        r.transform.position = instantiatePos;
    }
    
    /// <summary>
    /// Creates output resources according to the recipe and adds them to the outputBuffer.
    /// </summary>
    protected void MoveResourcesIn() {
        foreachMachine(new List<MachinePort>(InputPorts), m => {
            OutputBuffer.AddRange(m.OutputBuffer);
            m.OutputBuffer.Clear();
        });
        
        foreach (Resource r in OutputBuffer) {
            MoveHere(r, _shouldDestroyInputs());
        }
    }
    
    /// <summary>
    /// Instantiates new resources to feed into the output machine.
    /// </summary>
    protected virtual void CreateOutput() {
        var position = transform.position;
        var resourcesToCreate = recipeObj.outToList();
        foreach (Resource r in resourcesToCreate) {
            var instantiatePos = new Vector3(position.x, position.y, r.transform.position.z);
            var newObj = Instantiate(r, instantiatePos, transform.rotation);
            if (_shouldPrint) {
                print("Adding new resource: " + r);
            }
            OutputBuffer.Add(newObj.GetComponent<Resource>());
        }
    }
    
    /// <summary>
    /// Returns true if the machine destroys its input resources after moving them in.
    /// </summary>
    /// <returns></returns>
    protected virtual bool _shouldDestroyInputs() {
        return recipeObj.CreatesNewOutput;
    }
    
    /// <summary>
    /// Moves all resources from each input machine into this machine, removing them from the input machine's storage.
    /// </summary>
    public void MoveAndDestroy() {
        //Foreach resource in each port's input buffer, move to this machine
        foreachMachine(new List<MachinePort>(InputPorts), m => {
            foreach (Resource resource in m.OutputBuffer) {
                if (_shouldPrint) {
                    print("moving resource: " + m + resource);
                }
                MoveHere(resource, _shouldDestroyInputs());
            }
        });
        //Empty the output list of the input machines
        if (_shouldPrint) {
            print("Emptying input ports' output");
        }
        foreachMachine(new List<MachinePort>(InputPorts), m => m.ClearOutput());
        //Create new resources based on the old ones
        if (_shouldPrint) {
            print("Creating output");
        }
        CreateOutput();
    }
    
    protected virtual void _produce() {
        if (recipeObj.CreatesNewOutput) {
            if (_shouldPrint) {
                print("moving and destroying");
            }
            MoveAndDestroy();
        } else {
            MoveResourcesIn();
        }
    }
    
    public void PrepareTick() {
        _pokedThisTick = false;
    }
    
    /// <summary>
    /// Completes one cycle of logic.
    /// Produces output if needed.
    /// </summary>
    public void Tick() {
        if (!_pokedThisTick) {
            _pokedThisTick = true;
            bool enoughInput = _checkEnoughInput();
            if (_shouldPrint) {
                print("Enough ticks: " + (_ticksSinceProduced >= recipeObj.ticks));
            }

            if (enoughInput && _ticksSinceProduced >= recipeObj.ticks) {
                _produce();
                _ticksSinceProduced = 0;
            } else {
                _ticksSinceProduced++;
            }
            foreachMachine(new List<MachinePort>(InputPorts), m => m.Tick());
        }

        if (_shouldBreak) {
            Debug.Break();
        }
    }
    
    /// <summary>
    /// Unity Specific function. Defines how debug arrows are drawn.
    /// </summary>
    public void OnDrawGizmos() {
        Vector3 curPos = transform.position + new Vector3(0.1f, 0.1f, 0);
        foreachMachine(new List<MachinePort>(OutputPorts), m => {
            Vector3 direction = m.transform.position +new Vector3(0.1f, 0.1f, 0) - curPos;
            Helper.DrawArrow(curPos, direction, Color.green);
        });
        foreachMachine(new List<MachinePort>(InputPorts), m => {
            Vector3 direction = curPos-m.transform.position - new Vector3(0.1f, 0.1f, 0);
            Helper.DrawArrow(m.transform.position, direction, Color.blue);
        });
    }

    public int GetNumOutputMachines() {
        int ret = 0;
        foreach (OutputPort p in OutputPorts) {
            if (p.ConnectedMachine != null) ret++;
        }
        return ret;
    }
    
    /// <summary>
    /// Creates a new output port that feeds into <paramref name="m"/>.
    /// Also creates new input port on <paramref name="m"/> that feeds from this machine.
    /// </summary>
    /// <param name="m">The machine to connect to</param>
    public void AddOutputMachine(Machine m) {
        Vector3 portPos = (m.transform.position + transform.position) / 2;
        OutputPort newPort = Conductor.GetPooler().InstantiateOutputPort(portPos, transform);
        newPort.ConnectedMachine = m;
        OutputPorts = new List<OutputPort>();
        OutputPorts.Add(newPort);
        
        m.AddInputMachine(this);
    }
    
    /// <summary>
    /// Creates a new input port that feeds from <paramref name="m"/>.
    /// </summary>
    /// <param name="m">The machine to connect to</param>
    public void AddInputMachine(Machine m) {
        Vector3 portPos = (m.transform.position + transform.position) / 2;
        InputPort newPort = Conductor.GetPooler().InstantiateInputPort(portPos, transform);
        newPort.ConnectedMachine = m;
        InputPorts.Add(newPort);
    }

    public override void OnInteract(PlayerController p) {
        // throw new NotImplementedException();
    }

    public override void OnDeInteract(PlayerController p) {
        _dragDirection = Vector2.zero;
        Interactable onInteractable = p.OnInteractable();
        Machine onMachine = null;
        if (onInteractable != null) {
            onMachine = onInteractable.gameObject.GetComponent<Machine>();
        }
        List<Machine> conveyors = InstantiateFromBluePrints(_dragBluePrints, onMachine);
        ClearDragBluePrints();
        ConfigureDragPorts(conveyors, onMachine);
    }
    
    
    /** <summary>
     *      For each blueprint in [dragBluePrints], instantiate a new machine
     *      Assumes the blueprint list is ordered by distance from this machine
     * </summary>
     **/
    public List<Machine> InstantiateFromBluePrints(List<MachineBluePrint> dragBluePrints, Machine onMachine) {
        List<Machine> ret = new List<Machine>();
        for (int i = 0; i < dragBluePrints.Count; i++) {
            MachineBluePrint bluePrint = dragBluePrints[i];
            Transform bluePrintTransform = bluePrint.transform;

            // If this is the last conveyor and the player is on a machine,
            // add onMachine to the return list, then break
            if (i == dragBluePrints.Count - 1 && onMachine) {
                ret.Add(onMachine);
                break;
            }
            
            // if (bluePrintTransform.position)
            // RaycastHit

            // Instantiate a new conveyor
            Machine instMachine = Conductor.GetPooler().InstantiateConveyor(
                bluePrintTransform.position,
                bluePrintTransform.rotation
            );
            //TODO: make more efficient
            instMachine.GetComponentInChildren<SpriteRenderer>().sprite =
                bluePrint.GetComponentInChildren<SpriteRenderer>().sprite;
            ret.Add(instMachine);
        }
        return ret;
    }

    /**
     * <summary>
     *      Sets the input and output ports of each conveyor in [conveyors].
     *      Treats the onMachine like another conveyor.
     * </summary>
     */
    public void ConfigureDragPorts(List<Machine> conveyors, Machine onMachine) {
        for (int i = 0; i < conveyors.Count; ++i) {
            Machine curMachine = conveyors[i];
            // If this is the first conveyor in the line, set the machine to it.
            if (i == 0) {
                AddOutputMachine(curMachine);
            }

            // If this is the last conveyor in the line and the player is on a machine,
            // Set the output of the new conveyor to the new machine
            if (i >= conveyors.Count - 1) {
                break;
            } else {
                curMachine.AddOutputMachine(conveyors[i+1]);
            }
        }
    }

    public override void OnDrag(PlayerController p, Vector3 newPos) {
        
        ClearDragBluePrints();
        
        Vector2 delta = newPos - transform.position;
        _dragDirection = GetNewInitDragDirection(_dragDirection, delta);
        
        // Get the component of delta in the direction of dir
        // Then draw blueprints in that direction
        int n1 = (int)Math.Abs(Vector2.Dot(delta, _dragDirection));
        
        Vector3 startPos2 = transform.position + (Vector3)_dragDirection * n1;
        Vector2 orthoDir = delta - n1*_dragDirection;
        int n2 = (int) Math.Abs(orthoDir.x + orthoDir.y);
        orthoDir.Normalize();

        _dragBluePrints.AddRange(RenderConveyorBluePrintLine(n1, transform.position, _dragDirection, orthoDir));
        
        // Get the component of delta orthogonal to the direction of dir
        // Then draw blueprints in that direction
        if (n2 != 0) {
            _dragBluePrints.AddRange(RenderConveyorBluePrintLine(n2, startPos2, orthoDir));
        }
    }

    public void ClearDragBluePrints() {
        foreach (MachineBluePrint m in _dragBluePrints) {
            Destroy(m.gameObject);
        }
        _dragBluePrints.Clear();
    }

    /// <summary>
    /// Draws <paramref name="n"/> conveyors starting at <paramref name="startPos"/>,
    /// going in direction <paramref name="dir"/>
    /// </summary>
    /// <returns>List of the conveyor belt blueprints to draw</returns>
    public List<ConveyorBlueprint> RenderConveyorBluePrintLine(int n, Vector3 startPos, Vector2 dir, Vector2 orthoDir) {
        List<ConveyorBlueprint> ret = new List<ConveyorBlueprint>();
        // Account for dir.x being 0 which causes a div by 0 error
        print(dir + " " + orthoDir);
        for (int i = 1; i < n+1; ++i) {
            ConveyorBlueprint add = Conductor.GetPooler().CreateConveyorBluePrint(startPos + (Vector3) (dir * i));
            if (i < n) {
                add.SetEdgeSprite(dir);
            } else if(orthoDir != Vector2.zero) {
                add.SetCornerSprite(dir, orthoDir);
            } else {
                add.SetEdgeSprite(dir);
            }

            ret.Add(add);
        }
        return ret;
    }
    
    public List<ConveyorBlueprint> RenderConveyorBluePrintLine(int n, Vector3 startPos, Vector2 dir) {
        return RenderConveyorBluePrintLine(n, startPos, dir, Vector2.zero);
    }
}