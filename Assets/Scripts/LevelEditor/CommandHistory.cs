using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CommandHistory
{
    private List<ICommand> commandHistory = new List<ICommand>();
    private int index;

    public void AddCommand(ICommand command)
    {
        if (index < commandHistory.Count) 
            commandHistory.RemoveRange(index, commandHistory.Count - index);

        commandHistory.Add(command);
        command.Execute();
        index++;
    }

    public void UndoCommand()
    {
        if (commandHistory.Count == 0) {
            Debug.Log("Nothing to undo");
            return;
        }
        if (index > 0) {
            Debug.Log("Undo!");
            index--;
            commandHistory[index].Undo();
        } else {
            Debug.Log("Nothing to undo");
        }
    }

    public void RedoCommand()
    {
        if (commandHistory.Count == 0) { 
            Debug.Log("No more to redo");
            return;
        }

        if (index < commandHistory.Count) {
            Debug.Log("Redo!");
            commandHistory[index].Execute();
            index++;
        } else {
            Debug.Log("No more to redo");
        }
    }

    public void Clear()
    {
        commandHistory.Clear();
        index = 0;
    }
}
