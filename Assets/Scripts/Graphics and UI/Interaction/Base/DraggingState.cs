﻿using UnityEngine;

public class DraggingState : InteractableState {
    public override void SetZPressed(bool zPressed) {
        if (!zPressed) {
            MyStateMachine.Transition<NotInteractingState>();
        }
    }

    public override void Move() {
        ((Draggable)MyStateMachine.CurInput.CurInteractable).OnDrag(
            MyStateMachine.CurInput.PC,
            MyStateMachine.CurInput.NewPos
        );
    }

    public override void SetXPressed(bool xPressed)
    {
    }
    public override void Enter(InteractableStateInput i) {
        ((Draggable)(MyStateMachine.CurInput.CurInteractable)).OnDrag(MyStateMachine.CurInput.PC, MyStateMachine.CurInput.NewPos);
    }

    public override void Exit(InteractableStateInput i) {
        
    }

    public override void Update(InteractableStateInput i) {
        
    }
}