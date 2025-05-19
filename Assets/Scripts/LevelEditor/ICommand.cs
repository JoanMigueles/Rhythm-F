using System.Collections.Generic;

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
        EditorManager em = NoteManager.instance as EditorManager;
        if (em == null) return;
        foreach (NoteData note in createNotes) {
            em.SpawnNote(note, selectNotes);
        }
    }

    public void Undo() 
    {
        EditorManager em = NoteManager.instance as EditorManager;
        if (em == null) return;
        foreach (NoteData note in createNotes) {
            em.DeleteNote(note);
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
        EditorManager em = NoteManager.instance as EditorManager;
        if (em == null) return;
        em.SpawnMarker(flag);
    }

    public void Undo()
    {
        EditorManager em = NoteManager.instance as EditorManager;
        if (em == null) return;
        em.DeleteMarker(flag);
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
        EditorManager em = NoteManager.instance as EditorManager;
        if (em == null) return;
        foreach (NoteData note in deleteNotes) {
            em.DeleteNote(note);
        }
    }

    public void Undo()
    {
        EditorManager em = NoteManager.instance as EditorManager;
        if (em == null) return;
        em.ClearSelection();
        foreach (NoteData note in deleteNotes) {
            em.SpawnNote(note, true);
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
        EditorManager em = NoteManager.instance as EditorManager;
        if (em == null) return;
        em.DeleteMarker(flag);
    }

    public void Undo()
    {
        EditorManager em = NoteManager.instance as EditorManager;
        if (em == null) return;
        em.SpawnMarker(flag);
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
        EditorManager em = NoteManager.instance as EditorManager;
        if (em == null) return;
        for (int i = 0; i < previousNotes.Length; i++) {
            em.EditNote(previousNotes[i], editedNotes[i]);
        }
    }

    public void Undo()
    {
        EditorManager em = NoteManager.instance as EditorManager;
        if (em == null) return;
        for (int i = 0; i < previousNotes.Length; i++) {
            em.EditNote(editedNotes[i], previousNotes[i]);
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
        EditorManager em = NoteManager.instance as EditorManager;
        if (em == null) return;
        em.EditMarker(flag, newFlag);
    }

    public void Undo()
    {
        EditorManager em = NoteManager.instance as EditorManager;
        if (em == null) return;
        em.EditMarker(newFlag, flag);
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