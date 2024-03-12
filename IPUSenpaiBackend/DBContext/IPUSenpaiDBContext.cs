using System;
using System.Collections.Generic;
using IPUSenpaiBackend.Entities;
using Microsoft.EntityFrameworkCore;

namespace IPUSenpaiBackend.DBContext;

public partial class IPUSenpaiDBContext : DbContext
{
    public IPUSenpaiDBContext()
    {
    }

    public IPUSenpaiDBContext(DbContextOptions<IPUSenpaiDBContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Institute> Institutes { get; set; }

    public virtual DbSet<Programme> Programmes { get; set; }

    public virtual DbSet<ProgrammesInstitute> ProgrammesInstitutes { get; set; }

    public virtual DbSet<Result> Results { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<Subject> Subjects { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.LogTo(Console.WriteLine);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Institute>(entity =>
        {
            entity.HasKey(e => e.Instcode).HasName("institute_pkey");

            entity.ToTable("institute");

            entity.HasIndex(e => e.Instname, "instname_indx");

            entity.Property(e => e.Instcode)
                .ValueGeneratedNever()
                .HasColumnName("instcode");
            entity.Property(e => e.Instname).HasColumnName("instname");
        });

        modelBuilder.Entity<Programme>(entity =>
        {
            entity.HasKey(e => e.Progcode).HasName("programme_pkey");

            entity.ToTable("programme");

            entity.HasIndex(e => e.Prog, "prog_indx");

            entity.HasIndex(e => e.Spec, "spec_indx");

            entity.Property(e => e.Progcode)
                .HasMaxLength(8)
                .HasColumnName("progcode");
            entity.Property(e => e.Prog).HasColumnName("prog");
            entity.Property(e => e.Progname).HasColumnName("progname");
            entity.Property(e => e.Spec).HasColumnName("spec");
        });

        modelBuilder.Entity<ProgrammesInstitute>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("programmes_institutes");

            entity.HasIndex(e => new { e.Instcode, e.Progcode }, "programmes_institutes_instcode_progcode_key").IsUnique();

            entity.Property(e => e.Instcode).HasColumnName("instcode");
            entity.Property(e => e.Progcode)
                .HasMaxLength(8)
                .HasColumnName("progcode");

            entity.HasOne(d => d.InstcodeNavigation).WithMany()
                .HasForeignKey(d => d.Instcode)
                .HasConstraintName("programmes_institutes_instcode_fkey");

            entity.HasOne(d => d.ProgcodeNavigation).WithMany()
                .HasForeignKey(d => d.Progcode)
                .HasConstraintName("programmes_institutes_progcode_fkey");
        });

        modelBuilder.Entity<Result>(entity =>
        {
            entity.HasKey(e => new { e.ResultId, e.Enrolno, e.Subcode, e.Schemeid, e.Exam }).HasName("results_pkey");

            entity.ToTable("results");

            entity.HasIndex(e => e.Enrolno, "enrolno_indx");

            entity.HasIndex(e => e.Semester, "semester_indx");

            entity.HasIndex(e => e.Subcode, "subcode_indx");

            entity.Property(e => e.ResultId)
                .ValueGeneratedOnAdd()
                .HasColumnName("result_id");
            entity.Property(e => e.Enrolno)
                .HasMaxLength(12)
                .HasColumnName("enrolno");
            entity.Property(e => e.Subcode)
                .HasMaxLength(12)
                .HasColumnName("subcode");
            entity.Property(e => e.Schemeid)
                .HasMaxLength(15)
                .HasColumnName("schemeid");
            entity.Property(e => e.Exam).HasColumnName("exam");
            entity.Property(e => e.Batch).HasColumnName("batch");
            entity.Property(e => e.External).HasColumnName("external");
            entity.Property(e => e.Internal).HasColumnName("internal");
            entity.Property(e => e.Resultdate).HasColumnName("resultdate");
            entity.Property(e => e.Semester).HasColumnName("semester");
            entity.Property(e => e.Total).HasColumnName("total");

            entity.HasOne(d => d.EnrolnoNavigation).WithMany(p => p.Results)
                .HasForeignKey(d => d.Enrolno)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("results_enrolno_fkey");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Enrolno).HasName("student_pkey");

            entity.ToTable("student");

            entity.HasIndex(e => e.Instcode, "instcode_indx");

            entity.HasIndex(e => e.Name, "name_indx");

            entity.HasIndex(e => e.Progcode, "progcode_indx");

            entity.Property(e => e.Enrolno)
                .HasMaxLength(12)
                .HasColumnName("enrolno");
            entity.Property(e => e.Batch).HasColumnName("batch");
            entity.Property(e => e.Instcode).HasColumnName("instcode");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Progcode)
                .HasMaxLength(12)
                .HasColumnName("progcode");
            entity.Property(e => e.Sid)
                .HasMaxLength(20)
                .HasColumnName("sid");

            entity.HasOne(d => d.InstcodeNavigation).WithMany(p => p.Students)
                .HasForeignKey(d => d.Instcode)
                .HasConstraintName("student_instcode_fkey");

            entity.HasOne(d => d.ProgcodeNavigation).WithMany(p => p.Students)
                .HasForeignKey(d => d.Progcode)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("student_progcode_fkey");
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.Subid).HasName("subjects_pkey");

            entity.ToTable("subjects");

            entity.HasIndex(e => new { e.Subcode, e.Paperid, e.Papername, e.Credits, e.Minor, e.Major, e.Mode, e.Kind, e.Maxmarks, e.Passmarks, e.Schemeid, e.Type, e.Exam }, "unique_subjects_constraint").IsUnique();

            entity.Property(e => e.Subid).HasColumnName("subid");
            entity.Property(e => e.Credits).HasColumnName("credits");
            entity.Property(e => e.Exam)
                .HasMaxLength(12)
                .HasColumnName("exam");
            entity.Property(e => e.Kind)
                .HasMaxLength(15)
                .HasColumnName("kind");
            entity.Property(e => e.Major).HasColumnName("major");
            entity.Property(e => e.Maxmarks).HasColumnName("maxmarks");
            entity.Property(e => e.Minor).HasColumnName("minor");
            entity.Property(e => e.Mode)
                .HasMaxLength(15)
                .HasColumnName("mode");
            entity.Property(e => e.Paperid)
                .HasMaxLength(12)
                .HasColumnName("paperid");
            entity.Property(e => e.Papername).HasColumnName("papername");
            entity.Property(e => e.Passmarks).HasColumnName("passmarks");
            entity.Property(e => e.Schemeid)
                .HasMaxLength(15)
                .HasColumnName("schemeid");
            entity.Property(e => e.Subcode)
                .HasMaxLength(12)
                .HasColumnName("subcode");
            entity.Property(e => e.Type)
                .HasMaxLength(15)
                .HasColumnName("type");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
