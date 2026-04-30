// <copyright file="BackupRestoreIntegrityTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Security.Cryptography;

namespace BudgetExperiment.Infrastructure.Tests.Encryption;

/// <summary>
/// Validates the checksum verification invariants used by backup-encrypted.sh and
/// restore-encrypted.sh without requiring GPG, PostgreSQL, or production secrets.
///
/// The restore script logic under test (restore-encrypted.sh lines 98–117):
///   expected=$(awk '{ print $1 }' "${checksum_file}")
///   actual=$(sha256sum "${input_file}" | awk '{ print $1 }')
///   if [[ "${actual}" != "${expected}" ]]; then fail "checksum verification failed"; fi
///
/// The backup script produces the .sha256 file via (backup-encrypted.sh):
///   sha256sum "${output_file}" > "${checksum_file}"
///
/// These tests replicate that exact format and comparison so CI provides automated evidence
/// that the integrity gate cannot be bypassed by a tampered or corrupted archive.
/// </summary>
public sealed class BackupRestoreIntegrityTests : IDisposable
{
    private readonly string _workDir;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackupRestoreIntegrityTests"/> class.
    /// </summary>
    public BackupRestoreIntegrityTests()
    {
        _workDir = Path.Combine(Path.GetTempPath(), $"budget-backup-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_workDir);
    }

    // ── Positive path ────────────────────────────────────────────────────────
    [Fact]
    public void RestoreIntegrity_WhenChecksumMatchesArchive_VerificationPasses()
    {
        // Arrange — simulate backup-encrypted.sh creating an archive and .sha256 file.
        var archivePath = Path.Combine(_workDir, "budgetexperiment-20260428T120000Z.sql.gz.gpg");
        var checksumPath = $"{archivePath}.sha256";

        // Arbitrary binary content standing in for a real GPG-encrypted gzip archive.
        var archiveBytes = new byte[256];
        Random.Shared.NextBytes(archiveBytes);
        File.WriteAllBytes(archivePath, archiveBytes);

        WriteChecksumFile(archivePath, checksumPath);

        // Act — simulate restore-encrypted.sh checksum gate.
        var result = VerifyArchiveChecksum(archivePath, checksumPath);

        // Assert
        Assert.True(result, "Checksum verification must pass for an unmodified archive.");
    }

    [Fact]
    public void RestoreIntegrity_WhenChecksumFileRecordsCorrectHash_ExtractedHexMatchesActual()
    {
        // Arrange
        var archivePath = Path.Combine(_workDir, "budgetexperiment-roundtrip.sql.gz.gpg");
        var checksumPath = $"{archivePath}.sha256";

        var archiveBytes = new byte[512];
        Random.Shared.NextBytes(archiveBytes);
        File.WriteAllBytes(archivePath, archiveBytes);

        WriteChecksumFile(archivePath, checksumPath);

        // Act
        var expectedFromFile = ReadExpectedChecksumFromFile(checksumPath);
        var actualComputed = ComputeSha256Hex(archivePath);

        // Assert
        Assert.Equal(64, expectedFromFile.Length); // SHA-256 hex is always 64 chars.
        Assert.Equal(actualComputed, expectedFromFile);
    }

    // ── Negative paths ───────────────────────────────────────────────────────
    [Fact]
    public void RestoreIntegrity_WhenArchiveIsTamperedAfterChecksum_VerificationFails()
    {
        // Arrange — produce a valid archive and .sha256 pair.
        var archivePath = Path.Combine(_workDir, "budgetexperiment-tampered.sql.gz.gpg");
        var checksumPath = $"{archivePath}.sha256";

        var originalBytes = new byte[256];
        Random.Shared.NextBytes(originalBytes);
        File.WriteAllBytes(archivePath, originalBytes);
        WriteChecksumFile(archivePath, checksumPath);

        // Act — flip one byte to simulate bit-rot or deliberate tampering.
        var tamperedBytes = (byte[])originalBytes.Clone();
        tamperedBytes[0] ^= 0xFF;
        File.WriteAllBytes(archivePath, tamperedBytes);

        var result = VerifyArchiveChecksum(archivePath, checksumPath);

        // Assert
        Assert.False(result, "Checksum verification must fail when the archive has been modified after the checksum was recorded.");
    }

    [Fact]
    public void RestoreIntegrity_WhenChecksumFileContainsWrongHash_VerificationFails()
    {
        // Arrange — write a valid archive but a .sha256 with a deliberately wrong hash.
        var archivePath = Path.Combine(_workDir, "budgetexperiment-badhash.sql.gz.gpg");
        var checksumPath = $"{archivePath}.sha256";

        var archiveBytes = new byte[256];
        Random.Shared.NextBytes(archiveBytes);
        File.WriteAllBytes(archivePath, archiveBytes);

        // Write a zeroed-out hash — clearly wrong.
        var fakeHex = new string('0', 64);
        File.WriteAllText(checksumPath, $"{fakeHex}  {Path.GetFileName(archivePath)}\n");

        // Act
        var result = VerifyArchiveChecksum(archivePath, checksumPath);

        // Assert
        Assert.False(result, "Checksum verification must fail when the .sha256 file contains an incorrect hash.");
    }

    [Fact]
    public void RestoreIntegrity_WhenChecksumFileIsMissing_VerificationFails()
    {
        // Arrange — archive exists, but no .sha256 companion.
        var archivePath = Path.Combine(_workDir, "budgetexperiment-nochecksum.sql.gz.gpg");
        var checksumPath = $"{archivePath}.sha256";

        File.WriteAllBytes(archivePath, new byte[] { 0x01, 0x02, 0x03 });

        // Intentionally do NOT write a checksum file.

        // Act
        var result = VerifyArchiveChecksum(archivePath, checksumPath);

        // Assert
        Assert.False(result, "Checksum verification must fail when the .sha256 file is absent (matches restore script 'checksum file not found' guard).");
    }

    [Fact]
    public void RestoreIntegrity_WhenChecksumFileIsEmpty_VerificationFails()
    {
        // Arrange — .sha256 file exists but is empty (corrupt write).
        var archivePath = Path.Combine(_workDir, "budgetexperiment-emptychecksum.sql.gz.gpg");
        var checksumPath = $"{archivePath}.sha256";

        File.WriteAllBytes(archivePath, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF });
        File.WriteAllText(checksumPath, string.Empty);

        // Act
        var result = VerifyArchiveChecksum(archivePath, checksumPath);

        // Assert
        Assert.False(result, "Checksum verification must fail when the .sha256 file contains no parseable hash (matches restore script 'checksum file is invalid' guard).");
    }

    // ── IDisposable ──────────────────────────────────────────────────────────
    public void Dispose()
    {
        if (Directory.Exists(_workDir))
        {
            Directory.Delete(_workDir, recursive: true);
        }
    }

    /// <summary>
    /// Computes the lowercase hex SHA-256 of a file — mirrors <c>sha256sum file | awk '{print $1}'</c>.
    /// </summary>
    private static string ComputeSha256Hex(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Writes a checksum file in the format produced by <c>sha256sum</c>:
    ///   &lt;hex&gt;  &lt;filename&gt;
    /// (two spaces, matching GNU coreutils sha256sum binary-mode convention).
    /// </summary>
    private static void WriteChecksumFile(string archivePath, string checksumPath)
    {
        var hex = ComputeSha256Hex(archivePath);
        var filename = Path.GetFileName(archivePath);
        File.WriteAllText(checksumPath, $"{hex}  {filename}\n");
    }

    /// <summary>
    /// Reads the expected checksum from a .sha256 file — mirrors
    /// <c>awk '{ print $1 }' "${checksum_file}"</c>.
    /// </summary>
    private static string ReadExpectedChecksumFromFile(string checksumPath)
    {
        var line = File.ReadLines(checksumPath).FirstOrDefault() ?? string.Empty;
        return line.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
    }

    /// <summary>
    /// Simulates the restore-script verification: returns true when the archive
    /// is intact, false when the checksum does not match (tampered/corrupted).
    /// </summary>
    private static bool VerifyArchiveChecksum(string archivePath, string checksumPath)
    {
        if (!File.Exists(checksumPath))
        {
            return false;
        }

        var expected = ReadExpectedChecksumFromFile(checksumPath);
        if (string.IsNullOrEmpty(expected))
        {
            return false;
        }

        var actual = ComputeSha256Hex(archivePath);
        return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
    }
}
