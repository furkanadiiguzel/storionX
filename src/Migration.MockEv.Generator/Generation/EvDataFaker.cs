using System.Security.Cryptography;
using Bogus;
using EvStorionX.Domain.Entities;
using EvStorionX.Domain.Enums;
using EvStorionX.MockEv.Generator.Options;

namespace EvStorionX.MockEv.Generator.Generation;

/// <summary>
/// Produces all mock EV data from a single seeded <see cref="Faker"/> instance.
/// All randomness is deterministic: the same <paramref name="opts"/> seed always yields the same output.
/// </summary>
public sealed class EvDataFaker
{
    private static readonly string[] Providers =
        ["contoso.com", "fabrikam.com", "northwind.com", "acme.tr", "ev-corp.com", "litware.com"];

    private static readonly string[] VaultStores =
        ["VS-EMEA-01", "VS-US-WEST-01", "VS-APAC-01", "VS-EU-02"];

    private static readonly string[] MailboxFolders =
    [
        "Inbox", "Sent Items", "Inbox\\Projects", "Inbox\\Finance",
        "Inbox\\Legal", "Inbox\\HR", "Archive", "Inbox\\Customers",
        "Deleted Items", "Junk Email"
    ];

    private static readonly string[] TurkishSubjectPrefixes =
        ["İleri:", "Yanıt:", "Önemli:", "Acil:", "Bilgi için:", "RE:", "FW:", "RE: FW:"];

    private static readonly string[] TurkishSubjects =
    [
        "Aylık rapor gönderimi",
        "Toplantı daveti",
        "Fatura onayı gerekiyor",
        "Proje güncellemesi",
        "Acil: Sunucu bakımı",
        "Yeni politika duyurusu",
        "Haftalık özet",
        "Sözleşme imzası",
        "Bütçe onayı talebi",
        "Denetim raporu",
    ];

    private static readonly string[] RetentionCategories =
        ["Standard-3Y", "Legal-7Y", "Finance-10Y", "HR-5Y", "Compliance-7Y", "Archive-1Y"];

    private static readonly string[] MessageClasses =
        ["IPM.Note", "IPM.Note", "IPM.Note", "IPM.Note", "IPM.Appointment", "IPM.Task", "IPM.Contact"];

    public static GeneratedData Generate(GeneratorOptions opts)
    {
        var rng = new Faker("en") { Random = new Randomizer(opts.Seed) };

        var partsWithData = GenerateParts(opts.Parts, rng);
        var archives      = GenerateArchives(opts.Archives, rng);
        var (identityMap, orphanedUpns) = BuildIdentityMap(archives, rng, opts.Seed);
        var items         = GenerateItems(archives, opts, partsWithData, rng);

        return new GeneratedData(archives, items, partsWithData, identityMap, orphanedUpns);
    }

    // ── SIS Parts ────────────────────────────────────────────────────────────

    private static List<SisPartWithData> GenerateParts(int count, Faker rng)
    {
        var result = new List<SisPartWithData>(count);
        for (int i = 0; i < count; i++)
        {
            // Realistic attachment sizes: 1 KB – 512 KB
            var size    = rng.Random.Int(1_024, 524_288);
            var bytes   = rng.Random.Bytes(size);
            var sha256  = Convert.ToHexStringLower(SHA256.HashData(bytes));
            var partId  = $"ev-prt-{rng.Random.AlphaNumeric(12)}";
            var dataRef = $"ev://vault/{rng.PickRandom(VaultStores)}/parts/{partId}.bin";

            var part = new SisPart
            {
                PartId    = partId,
                Sha256    = sha256,
                SizeBytes = size,
                DataRef   = dataRef,
            };
            result.Add(new SisPartWithData(part, bytes));
        }
        return result;
    }

    // ── Archives ──────────────────────────────────────────────────────────────

    private static List<Archive> GenerateArchives(int count, Faker rng)
    {
        var orphanCount    = Math.Max(1, (int)Math.Ceiling(count * 0.15));
        var legalHoldCount = Math.Max(1, (int)Math.Ceiling(count * 0.10));

        // Shuffle indexes so legal-hold and orphan selection is spread, not front-loaded.
        var indexes = Enumerable.Range(0, count).ToArray();
        rng.Random.Shuffle(indexes);
        var legalHoldSet = indexes[..legalHoldCount].ToHashSet();
        // Orphan tracking is done in BuildIdentityMap; just mark a subset of mailbox archives here.
        // We store a flag via a separate orphan-index set used when building identity map.

        var result = new List<Archive>(count);
        for (int i = 0; i < count; i++)
        {
            ArchiveType type;
            if (i == 0)      type = ArchiveType.Journal;
            else if (i == 1) type = ArchiveType.Fsa;
            else             type = rng.Random.WeightedRandom(
                                        [ArchiveType.Mailbox, ArchiveType.Journal, ArchiveType.Fsa],
                                        [80, 10, 10]);

            string? ownerUpn = type switch
            {
                ArchiveType.Journal => null,
                ArchiveType.Fsa     => null,
                _                   => $"{rng.Internet.UserName()}" +
                                       $"@{rng.PickRandom(Providers)}",
            };

            result.Add(new Archive
            {
                ArchiveId  = $"ev-arc-{rng.Random.AlphaNumeric(10)}",
                Type       = type,
                OwnerUpn   = ownerUpn,
                LegalHold  = legalHoldSet.Contains(i),
                VaultStore = rng.PickRandom(VaultStores),
            });
        }
        return result;
    }

    // ── Identity Map ─────────────────────────────────────────────────────────

    private static (Dictionary<string, string> map, IReadOnlySet<string> orphaned)
        BuildIdentityMap(List<Archive> archives, Faker rng, int seed)
    {
        var mailboxArchives = archives
            .Where(a => a.OwnerUpn is not null)
            .ToList();

        var orphanCount = Math.Max(1, (int)Math.Ceiling(mailboxArchives.Count * 0.15));

        // Shuffle deterministically to pick which mailbox UPNs become orphans.
        var shuffled = mailboxArchives.ToArray();
        rng.Random.Shuffle(shuffled);
        var orphanedUpns = shuffled[..orphanCount]
            .Select(a => a.OwnerUpn!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in mailboxArchives.Where(a => !orphanedUpns.Contains(a.OwnerUpn!)))
        {
            map[a.OwnerUpn!] = $"storionx-arc-{rng.Random.AlphaNumeric(8)}";
        }

        return (map, orphanedUpns);
    }

    // ── Items ─────────────────────────────────────────────────────────────────

    private static List<Item> GenerateItems(
        List<Archive> archives,
        GeneratorOptions opts,
        List<SisPartWithData> parts,
        Faker rng)
    {
        // Build a weighted part pool: top 20% of parts are "popular" (5× weight).
        // This simulates common footers / recurring attachments being SIS-deduped heavily.
        var popularCount = Math.Max(1, (int)(parts.Count * 0.2));
        var pool = new List<string>(parts.Count * 3);
        for (int i = 0; i < parts.Count; i++)
        {
            var weight = i < popularCount ? 5 : 1;
            for (int w = 0; w < weight; w++)
                pool.Add(parts[i].Part.PartId);
        }

        var items = new List<Item>();
        foreach (var archive in archives)
        {
            var itemCount = rng.Random.Int(opts.ItemsPerArchiveMin, opts.ItemsPerArchiveMax);
            for (int j = 0; j < itemCount; j++)
                items.Add(GenerateItem(archive, pool, rng));
        }
        return items;
    }

    private static Item GenerateItem(Archive archive, List<string> partPool, Faker rng)
    {
        var sentRaw = rng.Date.Between(DateTime.UtcNow.AddYears(-6), DateTime.UtcNow);
        var sentUtc = DateTime.SpecifyKind(sentRaw, DateTimeKind.Utc);

        var folderPath = archive.Type switch
        {
            ArchiveType.Fsa     => $@"\\fileserver\shares\{rng.Commerce.Department()}\{sentUtc.Year}\Q{(sentUtc.Month - 1) / 3 + 1}",
            ArchiveType.Journal => $"Journal\\{sentUtc:yyyy-MM}",
            _                   => rng.PickRandom(MailboxFolders),
        };

        var numParts = rng.Random.Int(1, 5);
        var contentPartIds = Enumerable.Range(0, numParts)
            .Select(_ => rng.PickRandom(partPool))
            .Distinct()
            .ToList();

        var toCount = rng.Random.Int(1, 5);
        var to = Enumerable.Range(0, toCount)
            .Select(_ => GenerateEmail(rng))
            .ToList();

        var ccCount = rng.Random.Int(0, 3);
        var cc = Enumerable.Range(0, ccCount)
            .Select(_ => GenerateEmail(rng))
            .ToList();

        var bccCount = rng.Random.Bool(0.1f) ? rng.Random.Int(1, 2) : 0;
        var bcc = Enumerable.Range(0, bccCount)
            .Select(_ => GenerateEmail(rng))
            .ToList();

        return new Item
        {
            ItemId           = $"ev-itm-{rng.Random.AlphaNumeric(14)}",
            ArchiveId        = archive.ArchiveId,
            FolderPath       = folderPath,
            Subject          = GenerateSubject(rng),
            SentDateUtc      = sentUtc,
            From             = GenerateEmail(rng),
            To               = to,
            Cc               = cc,
            Bcc              = bcc,
            ContentPartIds   = contentPartIds,
            RetentionCategory = rng.PickRandom(RetentionCategories),
            SizeBytes        = rng.Random.Long(2_048, 2_097_152),
            MessageClass     = rng.PickRandom(MessageClasses),
        };
    }

    private static string GenerateSubject(Faker rng) =>
        rng.Random.Bool(0.35f)
            ? $"{rng.PickRandom(TurkishSubjectPrefixes)} {rng.PickRandom(TurkishSubjects)}"
            : $"{rng.PickRandom("FW:", "RE:", "RE: FW:", "")} {rng.Lorem.Sentence(3, 7)}".TrimStart();

    private static string GenerateEmail(Faker rng) =>
        rng.Internet.Email(
            firstName: rng.Name.FirstName(),
            lastName: rng.Name.LastName(),
            provider: rng.PickRandom(Providers));
}
