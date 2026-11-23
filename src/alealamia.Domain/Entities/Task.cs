using System;
using System.Collections.Generic;
using System.Text;
using TaskStatus = alealamia.Domain.Enums.TaskStatus;

namespace alealamia.Domain.Entities
{
    public class Task
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int TrackId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal RewardAmount { get; set; }
        public TaskStatus Status { get; set; }
        public int? ActualDurationDays { get; set; }
        public DateTime PublishedDeadline { get; set; }
        public DateTime? ExtensionDeadline { get; set; }
        public int? AssignedFreelancerId { get; set; } //fk
        public string CreatedByUserId { get; set; } = string.Empty; //fk admin
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public ICollection<FreelancerBlock> FreelancerBlocks { get; set; } = new HashSet<FreelancerBlock>();
        public ICollection<TaskRevision> TaskRevisions { get; set; } = new List<TaskRevision>();
        public ICollection<TaskSubmission> TaskSubmissions { get; set; } = new List<TaskSubmission>();
        public ICollection<FreelancerLedgerEntry> FreelancerLedgerEntries { get; set; } = new List<FreelancerLedgerEntry>();
        public ICollection<TaskRequiredSkill> TaskRequiredSkills { get; set; } = new List<TaskRequiredSkill>();
        public Project Project { get; set; } = null!;
        public Track Track { get; set; } = null!;
        public FreelancerProfile? FreelancerProfile { get; set; }
        public ICollection<TaskOffer> TaskOffers { get; set; } = new List<TaskOffer>();
        public ICollection<TaskStatusHistory> TaskStatusHistories { get; set; } = new List<TaskStatusHistory>();
        public ICollection<TaskReassignment> TaskReassignments { get; set; }= new List<TaskReassignment>();
        public ICollection<TaskFile> TaskFiles { get; set; } = new List<TaskFile>();


    }
}
