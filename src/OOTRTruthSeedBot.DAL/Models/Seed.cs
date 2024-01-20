#nullable disable
namespace OOTRTruthSeedBot.DAL.Models;

public partial class Seed
{
    public int Id { get; set; }

    public ulong CreatorId
    {
        get
        {
            return ulong.Parse(InternalCreatorId);
        }
        set
        {
            InternalCreatorId = value.ToString();
        }
    }

    public DateTime CreationDate
    {
        get
        {
            return DateTime.UnixEpoch.AddSeconds(InternalCreationDate);
        }
        set
        {
            InternalCreationDate = (int)(value - DateTime.UnixEpoch).TotalSeconds;
        }
    }

    public DateTime? UnlockedDate
    {
        get
        {
            return InternalUnlockedDate == null ? null :  DateTime.UnixEpoch.AddSeconds(InternalUnlockedDate.Value);
        }
        set
        {
            InternalUnlockedDate = value == null ? null : (int)(value.Value - DateTime.UnixEpoch).TotalSeconds;
        }
    }

    public bool IsGenerated
    {
        get
        {
            return (InternalState & 0x1) != 0;
        }
        set
        {
            InternalState &= ~0x1;
            if (value)
            {
                InternalState |= 0x1;
            }
        }
    }

    public bool IsUnlocked
    {
        get
        {
            return (InternalState & 0x2) != 0;
        }
        set
        {
            InternalState &= ~0x2;
            if (value)
            {
                InternalState |= 0x2;
            }
        }
    }

    public bool IsDeleted
    {
        get
        {
            return (InternalState & 0x4) != 0;
        }
        set
        {
            InternalState &= ~0x4;
            if (value)
            {
                InternalState |= 0x4;
            }
        }
    }

    public string InternalCreatorId { get; set; }

    public int InternalCreationDate { get; set; }

    public int? InternalUnlockedDate { get; set; }

    public int InternalState { get; set; }
}