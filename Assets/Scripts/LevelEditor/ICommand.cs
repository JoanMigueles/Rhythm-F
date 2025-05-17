using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;

public interface ICommand
{
    void Execute();

    void Undo();
}

public class CreateNotesCommand : ICommand
{
    NoteData[] createNotes;
    bool selectNotes;
    public CreateNotesCommand(List<NoteData> notes) 
    { 
        createNotes = notes.ToArray();
        selectNotes = false;
    }

    public CreateNotesCommand(NoteData note)
    {
        createNotes = new NoteData[] { note };
        selectNotes = false;
    }

    public CreateNotesCommand(List<NoteData> notes, bool select)
    {
        createNotes = notes.ToArray();
        selectNotes = select;
    }

    public void Execute()
    {
        foreach (NoteData note in createNotes) {
            EditorManager.instance.SpawnNote(note, selectNotes);
        }
    }

    public void Undo() 
    {
        EditorManager.instance.ClearSelection();
        foreach (NoteData note in createNotes) {
            EditorManager.instance.DeleteNote(note);
        }
    }
}

public class CreateMarkerCommand : ICommand
{
    BPMFlag flag;
    public CreateMarkerCommand(BPMFlag flag)
    {
        this.flag = flag;
    }

    public void Execute()
    {
        EditorManager.instance.SpawnMarker(flag);
    }

    public void Undo()
    {
        EditorManager.instance.DeleteMarker(flag);
    }
}



public class DeleteNotesCommand : ICommand
{
    NoteData[] deleteNotes;
    public DeleteNotesCommand(List<NoteData> notes)
    {
        deleteNotes = notes.ToArray();
    }

    public DeleteNotesCommand(NoteData note)
    {
        deleteNotes = new NoteData[] { note };
    }

    public void Execute()
    {
        foreach (NoteData note in deleteNotes) {
            EditorManager.instance.DeleteNote(note);
        }
    }

    public void Undo()
    {
        EditorManager.instance.ClearSelection();
        foreach (NoteData note in deleteNotes) {
            EditorManager.instance.SpawnNote(note, true);
        }
    }
}

public class DeleteMarkerCommand : ICommand
{
    BPMFlag flag;
    public DeleteMarkerCommand(BPMFlag flag)
    {
        this.flag = flag;
    }

    public void Execute()
    {
        EditorManager.instance.DeleteMarker(flag);
    }

    public void Undo()
    {
        EditorManager.instance.SpawnMarker(flag);
    }
}

public class EditNotesCommand : ICommand
{
    NoteData[] previousNotes;
    NoteData[] editedNotes;

    public EditNotesCommand(List<NoteData> notes, List<NoteData> edited)
    {
        previousNotes = notes.ToArray();
        editedNotes = edited.ToArray();
    }

    public void Execute()
    {
        for (int i = 0; i < previousNotes.Length; i++) {
            EditorManager.instance.EditNote(previousNotes[i], editedNotes[i]);
        }
    }

    public void Undo()
    {
        for (int i = 0; i < previousNotes.Length; i++) {
            EditorManager.instance.EditNote(editedNotes[i], previousNotes[i]);
        }
    }
}

public class EditMarkerCommand : ICommand
{
    BPMFlag flag;
    BPMFlag newFlag;

    public EditMarkerCommand(BPMFlag flag, BPMFlag newFlag)
    {
        this.flag = flag;
        this.newFlag = newFlag;
    }

    public void Execute()
    {
        EditorManager.instance.EditMarker(flag, newFlag);
    }

    public void Undo()
    {
        EditorManager.instance.EditMarker(newFlag, flag);
    }
}

/*
public class MoveMarkerCommand : ICommand
{
    BPMFlag flag;
    int distance;
    public MoveMarkerCommand(BPMFlag flag, int distance)
    {
        this.flag = flag;
        this.distance = distance;
    }

    public void Execute()
    {
        EditorManager.instance.MoveMarker(flag, distance);
    }

    public void Undo()
    {
        BPMFlag movedFlag = new BPMFlag(flag);
        movedFlag.offset += distance;
        EditorManager.instance.MoveMarker(movedFlag, -distance);
    }
}*/