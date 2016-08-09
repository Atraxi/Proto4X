package atraxi.core

import atraxi.core.entities.action.definitions.Action
import atraxi.core.entities.action.definitions.ActionAttack
import atraxi.core.entities.action.definitions.ActionBuild
import atraxi.core.entities.action.definitions.ActionMove
import atraxi.core.entities.action.definitions.ActionSetState

import java.util.ArrayList

class Player(var name: String?) {
    var metal = 0
    var money = 0//TODO: replace with whatever actual resources will be used
    private val actionSetStatesThisTurn = ArrayList<ActionSetState>()
    private val actionBuildsThisTurn = ArrayList<ActionBuild>()
    private val actionAttacksThisTurn = ArrayList<ActionAttack>()
    private val actionMovesThisTurn = ArrayList<ActionMove>()
    var isTurnFinished: Boolean = false
        private set

    @Throws(IllegalArgumentException::class)
    fun queueAction(action: Action) {
        if (action.isValid) {
            if (action is ActionSetState) {
                actionSetStatesThisTurn.add(action)
            } else if (action is ActionBuild) {
                actionBuildsThisTurn.add(action)
            } else if (action is ActionAttack) {
                actionAttacksThisTurn.add(action)
            } else if (action is ActionMove) {
                actionMovesThisTurn.add(action)
            } else {
                throw IllegalArgumentException("Unsupported Action type:" + action.javaClass + ". Must extend ActionBuild, ActionAttack, or ActionMove")
            }
        } else {
            throw UnsupportedOperationException("Invalid Action")
        }
    }

    fun processSetStatesThisTurn() {
        //TODO: modify to keep a change log (or leave it to be done implicitly elsewhere?)
        actionSetStatesThisTurn.forEach(Consumer<ActionSetState> { it.execute() })
        actionSetStatesThisTurn.clear()
    }

    fun processBuildsThisTurn() {
        actionBuildsThisTurn.forEach(Consumer<ActionBuild> { it.execute() })
        actionBuildsThisTurn.clear()
    }

    fun processAttacksThisTurn() {
        actionAttacksThisTurn.forEach(Consumer<ActionAttack> { it.execute() })
        actionAttacksThisTurn.clear()
    }

    fun processMovesThisTurn() {
        actionMovesThisTurn.forEach(Consumer<ActionMove> { it.execute() })
        actionMovesThisTurn.clear()
    }

    fun doWork(timeDiff: Long, paused: Boolean) {
    }

    fun endTurn() {
        isTurnFinished = true
    }
}
