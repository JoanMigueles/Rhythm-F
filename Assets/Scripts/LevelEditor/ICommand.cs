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
        foreach (NoteData note in createNotes) {
            NoteManager.instance.SpawnNote(note, selectNotes);
        }
        

    }

    public void Undo() 
    {
        NoteManager.instance.ClearSelection();
        foreach (NoteData note in createNotes) {
            NoteManager.instance.DeleteNote(note);
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
        NoteManager.instance.SpawnMarker(flag);
    }

    public void Undo()
    {
        NoteManager.instance.DeleteMarker(flag);
    }
}

public class EditMarkerCommand : ICommand
{
    BPMFlag flag;
    float newBPM;

    public EditMarkerCommand(BPMFlag flag, float BPM)
    {
        this.flag = flag;
        newBPM = BPM;
    }

    public void Execute()
    {
        NoteManager.instance.EditMarker(flag, newBPM);
    }

    public void Undo()
    {
        BPMFlag editedFlag = new BPMFlag(flag);
        editedFlag.BPM = newBPM;
        NoteManager.instance.EditMarker(editedFlag, flag.BPM);
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
            NoteManager.instance.DeleteNote(note);
        }
    }

    public void Undo()
    {
        NoteManager.instance.ClearSelection();
        foreach (NoteData note in deleteNotes) {
            NoteManager.instance.SpawnNote(note, true);
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
        NoteManager.instance.DeleteMarker(flag);
    }

    public void Undo()
    {
        NoteManager.instance.SpawnMarker(flag);
    }
}

public class MoveNotesCommand : ICommand
{
    NoteData[] moveNotes;
    int distance;
    bool laneSwap;
    public MoveNotesCommand(List<NoteData> notes, int distance, bool laneSwap)
    {
        moveNotes = notes.ToArray();
        this.distance = distance;
        this.laneSwap = laneSwap;
    }

    public void Execute()
    {
        foreach (NoteData note in moveNotes) {
            NoteManager.instance.MoveNote(note, distance, laneSwap);
        }
    }

    public void Undo()
    {
        foreach (NoteData note in moveNotes) {
            NoteData movedNote = new NoteData(note);
            movedNote.time += distance;
            if (laneSwap) {
                movedNote.lane = movedNote.lane == 0 ? 1 : 0;
            }
            NoteManager.instance.MoveNote(movedNote, -distance, laneSwap);
        }
    }
}

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
        NoteManager.instance.MoveMarker(flag, distance);
    }

    public void Undo()
    {
        BPMFlag movedFlag = new BPMFlag(flag);
        movedFlag.offset += distance;
        NoteManager.instance.MoveMarker(movedFlag, -distance);
    }
}