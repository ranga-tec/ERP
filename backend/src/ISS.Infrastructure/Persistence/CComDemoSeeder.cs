using ISS.Domain.MasterData;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ISS.Infrastructure.Persistence;

internal static class CComDemoSeeder
{
    private const string DisabledCategoryCode = "CCOM-DISABLED";

    public static async Task<bool> SeedAsync(IssDbContext dbContext, CancellationToken cancellationToken)
    {
        var hasChanges = false;

        var company = await dbContext.Companies.FirstOrDefaultAsync(
            x => x.Code == CompanyDefaults.CComCompanyCode,
            cancellationToken);
        if (company is null)
        {
            company = new Company(CompanyDefaults.CComCompanyCode, CompanyDefaults.CComCompanyName)
            {
                Id = CompanyDefaults.CComCompanyId
            };
            await dbContext.Companies.AddAsync(company, cancellationToken);
            hasChanges = true;
        }

        var category = await dbContext.ItemCategories.FirstOrDefaultAsync(
            x => x.CompanyId == company.Id && x.Code == DisabledCategoryCode,
            cancellationToken);
        if (category is null)
        {
            category = new ItemCategory(company.Id, DisabledCategoryCode, "C-COM Disabled Category");
            category.Update(company.Id, DisabledCategoryCode, "C-COM Disabled Category", isActive: false);
            await dbContext.ItemCategories.AddAsync(category, cancellationToken);
            hasChanges = true;
        }
        else if (category.IsActive)
        {
            category.Update(company.Id, category.Code, category.Name, isActive: false, category.RevenueAccountId, category.ExpenseAccountId);
            hasChanges = true;
        }

        var suppliersByCode = await dbContext.Suppliers
            .Where(x => x.CompanyId == company.Id)
            .ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);
        foreach (var seed in SupplierSeeds)
        {
            if (suppliersByCode.TryGetValue(seed.Code, out var existing))
            {
                if (existing.CompanyId != company.Id
                    || existing.Code != seed.Code
                    || existing.Name != seed.Name
                    || existing.Phone is not null
                    || existing.Email is not null
                    || existing.Address != seed.Address
                    || !existing.IsActive)
                {
                    existing.Update(company.Id, seed.Code, seed.Name, phone: null, email: null, seed.Address, isActive: true);
                    hasChanges = true;
                }

                continue;
            }

            var created = new Supplier(company.Id, seed.Code, seed.Name, phone: null, email: null, seed.Address);
            await dbContext.Suppliers.AddAsync(created, cancellationToken);
            suppliersByCode[seed.Code] = created;
            hasChanges = true;
        }

        var itemsBySku = await dbContext.Items
            .Where(x => x.CompanyId == company.Id)
            .ToDictionaryAsync(x => x.Sku, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var seenSkuCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var seed in ItemSeeds)
        {
            var sku = GetDemoSku(seed.Sku, seenSkuCounts);
            if (itemsBySku.TryGetValue(sku, out var existing))
            {
                if (existing.CompanyId != company.Id
                    || existing.Sku != sku
                    || existing.Name != seed.Name
                    || existing.Type != ItemType.SparePart
                    || existing.TrackingType != TrackingType.None
                    || existing.UnitOfMeasure != "EA"
                    || existing.BrandId is not null
                    || existing.Barcode is not null
                    || existing.DefaultUnitCost != seed.DefaultUnitCost
                    || !existing.IsActive
                    || existing.CategoryId != category.Id
                    || existing.SubcategoryId is not null)
                {
                    existing.Update(
                        company.Id,
                        sku,
                        seed.Name,
                        ItemType.SparePart,
                        TrackingType.None,
                        "EA",
                        brandId: null,
                        barcode: null,
                        seed.DefaultUnitCost,
                        isActive: true,
                        category.Id,
                        subcategoryId: null);
                    hasChanges = true;
                }

                continue;
            }

            var created = new Item(
                company.Id,
                sku,
                seed.Name,
                ItemType.SparePart,
                TrackingType.None,
                "EA",
                brandId: null,
                barcode: null,
                seed.DefaultUnitCost,
                category.Id);
            await dbContext.Items.AddAsync(created, cancellationToken);
            itemsBySku[sku] = created;
            hasChanges = true;
        }

        return hasChanges;
    }

    private static string GetDemoSku(string sku, Dictionary<string, int> seenSkuCounts)
    {
        seenSkuCounts.TryGetValue(sku, out var count);
        count++;
        seenSkuCounts[sku] = count;
        return count == 1 ? sku : $"{sku}-{count}";
    }

    private static IReadOnlyList<ItemSeed> ItemSeeds => ParseItemSeeds();
    private static IReadOnlyList<SupplierSeed> SupplierSeeds => ParseSupplierSeeds();

    private static IReadOnlyList<ItemSeed> ParseItemSeeds()
        => ItemSeedTsv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line =>
            {
                var parts = line.Split('\t');
                return new ItemSeed(parts[0], parts[1], decimal.Parse(parts[2], CultureInfo.InvariantCulture));
            })
            .ToList();

    private static IReadOnlyList<SupplierSeed> ParseSupplierSeeds()
        => SupplierTsv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line =>
            {
                var parts = line.Split('\t');
                return new SupplierSeed(parts[0], parts[1], parts.Length > 2 ? parts[2] : null);
            })
            .ToList();

    private const string ItemSeedTsv = """
6675517	FILTER OIL ENGINE	4003.7585
6667352	FILTER FUEL W/SEPARATOR	5127.8360
6727475	CAP HYDRAULIC	4491.7445
6598492	FILTER AIR OUTER	8987.1687
6598362	FILTER AIR INNER	6214.4442
6661248	FILTER OIL HYDRAULIC	14120.4560
7319444	ELEMENT FILTER	18391.9505
6661022	FILTER OIL HYDRAULIC IN-LINE	10860.6312
6667322	BELT DRIVE	13841.8580
7100104	BELT ALTERNATOR	4527.9852
6715865	BUSHING	2317.2995
6691714	SENSOR, SEATBAR BICS	18879.3222
6680443	BUSHING MAGNET	7146.6117
6680441	BUSHING KEYED	1341.1223
6715860	ARM SEAT BAR	108288.5048
6715866	BUSHING	4105.6411
4F6106	PIN HARDENED CLEVIS	1341.1223
6715864	BUSHING	10227.0755
6716621	COVER, FUSE	2841.8611
6725302	GASKET	2249.4642
6731979	BUSHING WELD-ON	26893.1742
6730997	BUSHING WEAR	8647.3505
7121223	CAP DIESEL LOCKING	8923.8023
7286464	SENSOR, FUEL	11214.4036
6717402	BUSHING	1183.1498
7203382	PLUG STEEL	1578.0811
6657734	PUMP PRIMER HAND	10108.5961
7487057	KIT BRAKE WEDGE	10582.5136
6680970	VENT BREATHER	1222.6430
6553411	BUSHING PRESS FIT	551.2598
6564669	NUT WHEEL	788.2186
6569465	SHAFT, AXLE	68045.0105
7270107	HUB ASSEMBLY	65635.9298
7230495	LIFTARM BYPASS	40873.7405
6716843	WASHER	20692.7536
18C1636	SCREW HEX HD GR8	2802.3680
6737524	HANDLE CONTROL	2841.8611
6679837	COUPLER, FF MALE	47074.1611
6680018	COUPLER, FF MALE	85224.5198
7196894	KIT SEAL	19152.5217
7137770	KIT, SEAL CYLINDER	7462.5567
6665701	BUSHING PRESS FIT	2407.4367
6685060	BUSHING TORSION	3355.2717
6512026	BUSHING FLANGED	1222.6430
6671025	COIL SOLENOID	14966.2505
6676029	SOLENOID LOCK SPOOL	44230.6561
7123964	KIT SEAL	52445.2261
7108991	TANK HYDRAULIC	177243.5011
7120881	SCREEN, FILLER	3592.2305
6728149	CAP HYDRAULIC NON VENTED	3671.2167
6736377	EXCHANGER, OIL COOLER	629795.2205
7113987	HOSE	9871.6373
6674798	KIT REPAIR	2644.3955
6673828	HOSE HYDRAULIC ASSY	50983.9805
6685625	HOSE	40004.8917
6674316	SWITCH PRESS OIL HYD	35779.1273
6599744	BALL JOINT	2920.8473
6649551	GAS SPRING	12873.1148
6685070	KIT SEAL COMPLETE	37753.7836
6682034	MOTOR DRIVE	371984.1005
6671520	O-RING	748.7255
6671516	KIT SEAL	102917.4398
7164323	MOTOR, HYD COOLING FAN	604445.2805
6698065	COIL SOLENOID	40636.7817
7010979	VALVE CHECK	127679.6292
6660260	SWITCH PRESS OIL HYD	49720.2005
6693245	SWITCH IGNITION	7383.5705
6693241	KEY IGNITION (2)	1696.5605
6690948	SWITCH PARK BRAKE	5961.8180
6674315	SWITCH PRESS OIL ENG	52050.2948
6691498	SOLENOID SHUTOFF FUEL	49917.6661
6718414	SENDER, TEMP ENG	8015.4605
6675155	FUSE 100 AMP	3987.1617
6675154	HOLDER	26182.2980
6679820	SWITCH RELAY MAGNETIC	2525.9161
6727869	SENDER,HYD TEMP	6317.2561
6680419	HANDLELH	51378.9117
6680418	HANDLE ASSEMBLYRH	88818.3942
6677489	REGULATOR ASSY	17612.2898
6677488	RECTIFIRE ASSY	21601.0955
6680376	SWITCH	25629.3942
6736362	EXCHANGER, WATER RADIATOR	501126.6192
6736379	TANK EXPANSION	34317.8817
6733429	CAP	5487.9005
7101090	HOSE	9476.7061
6716571	LOUVER, PLASTIC LH	4934.9967
6716573	LOUVER	3513.2442
7137824	MUFFLER	188814.9867
6703022	SPRING CLIP	2091.4917
6705106	WASHER 5	1341.1223
6661785	MOUNT ENGINE	8291.9123
7110276	SPACER, TAPERED	1775.5467
6661787	WASHER SNUBBING	4934.9967
6567962	WASHER	1538.5880
6630945	WASHER	946.1911
6557291	SWIVEL BALL JOINT	1736.0536
6689012	KIT GASKET UPPER T2 ENG	72349.7611
6689018	KIT GASKET LOWER TIER II ENGINE	110697.5855
6672190	VALVE, DRAIN	19823.9048
3974871	CONNECTOR	6040.8042
6685087	VALVE	42611.4380
6674173	COVER	32106.2667
6684854	FLANGE	198293.3367
6655213	SLEEVE	36924.4280
6684825	GUIDE, SOLENOID	9081.7748
6685476	SEAL	6988.6392
6598102	VALVE, PUMP INJ	9042.2817
7435295	CORD GLOW PLUG	5527.3936
6665371	SWITCH VACUUM	16466.9892
6672276	LENS, LIGHT RED	7225.5980
6658228	AXLE SEAL	8015.4605
7296603	SHAFT SEAL	42769.4105
6678226	SEAL, OIL	13070.5805
6660814	RING QUAD	1736.0536
6700463	CUP, SEAL	1933.5192
6651709	SEAL MECH RUBBER	1341.1223
6716601	PIN, PIVOT	17612.2898
7102117	TANK FUEL	95097.8011
6689649	CHAIN, DRIVE	125941.9317
6689651	CHAIN, DRIVE	81472.6730
7266558	SPOOL, LIFT LOCK	43796.2317
6815152	SPOOL, TILT	32027.2805
3974894	FLANGE	14452.8398
6684842	PIPE	39096.5498
3975337	COLLAR	28828.3373
7024826	SEAL, OIL	9516.1992
6653859	SLEEVE, WEAR	22035.5198
6684789	SEAL, OIL	8647.3505
3975441	GEAR CAMSHAFT	17651.7830
6684838	GOVERNOR	25194.9698
6684839	LEVER	13070.5805
6685526	PLATE CONTROL	52089.7880
6684823	SPRING	5171.9555
6598098	PIN	788.2186
6674169	PULLEY	19113.0286
6653916	HOLDER	35858.1136
6684832	LEVER	25905.8461
6689599	SPRING	10661.4998
6685514	SPRING	4342.5998
6655160	PLUG	867.2048
6655161	WASHER	748.7255
6655158	BOLT	5369.4211
6684874	COLLAR	3315.7786
3974514	SPRING 5	4737.5311
3974515	RETAINER	5329.9280
7414581	FILTER HYDRAULIC	31517.2366
7349400	BELT DRIVE	13702.4705
7143971	SEATBAR	91740.8855
7147815	BOOT	5092.9692
7196000	BUSHING, TAPERED	26142.8048
7279183	PIN PIVOT TAPERED	5922.3248
55CM16100	SCREW HEX	8726.3367
270000000000	WASHER	906.6980
6631067	SEAL OIL	1499.0948
7115769	RETAINER	4895.5036
7321657	CHAIN	37161.3867
7236275	KIT SEAL	8133.9398
6691341	KIT SEAL	67334.1342
7454715	EXCHANGER OIL WATER AIR	134748.8986
7496373	CAP CANISTER HYDRAULIC	34594.3336
7333266	BELT DRIVE	25905.8461
7331954	TENSIONER IDLER	68716.3936
7021023	KIT, SEAL	45217.9842
3974574	O-RING	8331.4055
7109712	SHIELD	2407.4367
7210574	SHIELD	2407.4367
6709215	BOB-TACH	827.7117
7137866	SEAL-KIT	4382.0930
7137867	SEAL-KIT	4382.0930
7021023	SEAL-KIT	2407.4367
""";

    private const string SupplierTsv = """
CCOM-L001	AGM Diesel Engineering	Fuel injector pumps and injector repairs
CCOM-L002	Ideal first choice pvt ltd	Fuel injector pumps and injector repairs
CCOM-L003	Sparklit motor traders	Hydraulic cylinder repairs
CCOM-L004	Welcome hydraulics	Hydraulic hoses fabrication
CCOM-L005	Thilina Engineers	Lath works
CCOM-L006	LPG	Lath works
CCOM-L007	LPG	Windscreen/Beadings
CCOM-L008	LPG	Bearing repairs
CCOM-L009	Hesara machinery	Line boring
CCOM-L010	Sagara hydraulics	Hydraulic seal fabrication
CCOM-L011	Edirisinghe brothers	Engine works
CCOM-L012	Viskam	Special welding
CCOM-L013	Fine finish	Special lath works
CCOM-L014	wego radiator	Radiator and cooler repairs
CCOM-L015	New dyno	Equipment rent
CCOM-L016	Agraa heavy machinery tyre	Solid tyre replacement
CCOM-L017	SNS products	Fiber works
CCOM-L018	Kumarasinghe engineering	Lath works
CCOM-L019	N. Air condition	A/C Works
CCOM-L020	707	Car carrirs hire
CCOM-L021	Havelock wash bar	Vehicle service
CCOM-L022	Dimo	Fuel injector pumps and injector repairs
CCOM-F001	Doosan Bobcat Inc.,	Brand: BOBCAT; Origin: USA; Doosan Bobcat North America is headquartered in West Fargo, North Dakota
CCOM-F002	Doosan Bobcat India Pvt Ltd's	Brand: BOBCAT; Origin: India; HTC Towers, No. 41, GST Road, Guindy, Chennai, Tamil Nadu, 600032
CCOM-F003	FAYAT Group	Brand: DYNAPAC; Origin: France; Founded in 1957 in France
CCOM-F004	FIORI GROUP S.p.A.	Brand: FIORI; Origin: Italy; Via per Ferrara, 7, 41034 Finale Emilia MO, Italy
CCOM-F005	Yantai Jisan Heavy Industry Ltd.	Brand: TGC; Origin: China; No 41 Jinfeng Road, Fushan District, Yantai City, China
CCOM-F006	Top Global Parts Co., Ltd.,	Brand: TGP; Origin: Korea; A-1111, Thinkfactory, 150 Yeongdeungpo-ro, Yeongdeungpo-gu, Seoul, Korea
CCOM-F007	Daedong Engineering & Machinery co,.ltd	Brand: Daedong; Origin: South Korea; 39, Songdeok-ro 126beon-gil, Daesong-myeon, Nam-gu, Pohang-si, Gyeongsangbuk-do, Korea.
""";

    private sealed record ItemSeed(string Sku, string Name, decimal DefaultUnitCost);
    private sealed record SupplierSeed(string Code, string Name, string? Address);
}
