using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;

namespace Okafor_.NET.Seed;

public static class NewsDataSeed
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.Posts.AnyAsync()) return;

        var posts = new List<Post>
        {
            new Post
            {
                Title = "Hospital Community Health Outreach Programme — April 2026",
                Slug = "community-health-outreach-april-2026",
                Summary = "Our mobile health teams visited three underserved communities this month, providing free screenings, vaccinations, and basic health education to over 400 residents.",
                IsFeatured = true,
                Content = "This month, the Boniface & Paulina Okafor Memorial Hospital extended its community health mission beyond the walls of our facility, reaching three rural and peri-urban communities in the surrounding district.\n\nOur mobile teams — comprising two general practitioners, two nurses, and a public health officer — conducted free blood pressure screenings, fasting blood sugar checks, and basic physical examinations for 412 community members over three days.\n\nKey findings from the outreach:\n- 37 individuals were identified as having uncontrolled hypertension and referred for follow-up care.\n- 14 participants were found to have elevated fasting blood glucose, prompting diabetes awareness counselling.\n- 89 children under five received overdue routine vaccinations.\n\nCommunity health education sessions were held on hygiene, nutrition, safe water practices, and the importance of routine checkups. Printed materials in English and local languages were distributed to participating households.\n\nThis outreach is part of our hospital's annual commitment to preventive care and health equity. We plan to return to these communities every quarter and expand coverage to two additional areas by mid-year.\n\nIf your organisation or community group would like to request a health outreach visit, please contact our Community Health Department at the hospital.",
                Published = true,
                CreatedAt = new DateTime(2026, 4, 20, 9, 0, 0, DateTimeKind.Utc)
            },
            new Post
            {
                Title = "Preparing for a Hospital Visit: What You Should Know",
                Slug = "preparing-for-a-hospital-visit",
                Summary = "A practical guide to help patients and families arrive prepared, reduce waiting times, and get the most from their consultation.",
                Content = "Whether this is your first visit or a routine follow-up, arriving prepared helps our clinical team care for you more effectively — and makes the experience less stressful for you.\n\nWhat to bring:\n\n1. Photo identification (national ID, passport, or driver's licence).\n2. Your hospital patient number if you have been seen here before.\n3. A list of your current medications, including dosage and frequency.\n4. Any previous test results, discharge summaries, or referral letters from your GP.\n5. Your insurance card or payment method.\n\nRegistration and check-in:\n\nPlease arrive at least 15 minutes before your appointment to allow time for registration. Our reception team will verify your details, confirm your appointment, and direct you to the appropriate department.\n\nIf you are a new patient, the registration process typically takes 10–15 minutes. You may also complete your registration online by calling ahead, which can shorten your wait time.\n\nFor procedures requiring fasting (such as blood tests or certain scans), please follow the specific instructions given to you by your doctor or our scheduling team. Do not eat or drink (other than water) unless told it is safe to do so.\n\nIf you need to cancel or reschedule, please contact us at least 24 hours in advance so we can offer your slot to another patient.\n\nFor any questions before your visit, our reception team is available Monday to Saturday, 8am to 6pm.",
                Published = true,
                CreatedAt = new DateTime(2026, 4, 14, 8, 30, 0, DateTimeKind.Utc)
            },
            new Post
            {
                Title = "Why Routine Checkups Matter — Even When You Feel Well",
                Slug = "importance-of-routine-checkups",
                Summary = "Many serious conditions — including hypertension, diabetes, and certain cancers — show no obvious symptoms in their early stages. Regular checkups are one of the most effective tools for early detection.",
                Content = "It is easy to put off a medical appointment when you feel fine. But many of the conditions that cause the most harm to long-term health develop silently, without obvious symptoms, until they are much harder to treat.\n\nConditions that benefit most from early detection:\n\n- Hypertension (high blood pressure): Often called the 'silent killer', it presents no symptoms in most people until a heart attack or stroke occurs.\n- Type 2 diabetes: Early-stage diabetes can be reversed or well managed with lifestyle changes — but many patients are unaware they have it.\n- High cholesterol: Completely symptomless until it causes a cardiovascular event.\n- Certain cancers: Cervical, breast, colorectal, and prostate cancers have significantly better outcomes when detected early through routine screening.\n\nWhat a routine checkup includes:\n\nDepending on your age, sex, and family history, a general checkup at our hospital typically includes a physical examination, blood pressure measurement, fasting blood glucose, lipid panel, and a review of your current medications and lifestyle. Your doctor may request additional tests based on your individual risk profile.\n\nHow often should you visit?\n\nFor adults under 40 with no known risk factors, a general health review every two years is typically sufficient. Adults over 40, those with a family history of chronic disease, or individuals with existing conditions should attend annually or as directed by their doctor.\n\nTo book a routine health review, use our online appointment request form or contact our scheduling team directly.",
                Published = true,
                CreatedAt = new DateTime(2026, 4, 7, 10, 0, 0, DateTimeKind.Utc)
            },
            new Post
            {
                Title = "Understanding Basic Laboratory Tests: A Patient's Guide",
                Slug = "understanding-basic-lab-testing",
                Summary = "Your doctor has requested blood work — but what do all those abbreviations mean? This guide explains the most common laboratory tests ordered at our hospital and what the results indicate.",
                Content = "Receiving a set of laboratory results can feel overwhelming, especially if you are unfamiliar with medical terminology. This guide covers the most common tests our Diagnostics & Laboratory department performs and what they are designed to measure.\n\nFull Blood Count (FBC):\nThis test measures the different components of your blood — red cells, white cells, and platelets. It helps detect anaemia, infections, and blood disorders. Low haemoglobin, for example, may indicate iron deficiency.\n\nFasting Blood Glucose (FBG):\nMeasures the amount of sugar in your blood after an overnight fast. A result above the normal range on two separate occasions may indicate diabetes or pre-diabetes.\n\nLipid Panel (Cholesterol Screen):\nMeasures total cholesterol, LDL ('bad') cholesterol, HDL ('good') cholesterol, and triglycerides. Results help your doctor assess your cardiovascular risk.\n\nLiver Function Tests (LFTs):\nA panel of tests that evaluate how well your liver is functioning. Elevated levels of certain enzymes (such as ALT or AST) may indicate liver stress from medication, alcohol, or infection.\n\nKidney Function (Urea & Creatinine):\nThese markers help assess how well your kidneys are filtering waste from your blood. They are commonly monitored in patients with diabetes, hypertension, or those taking certain medications.\n\nThyroid Function (TSH, T3, T4):\nThe thyroid gland regulates metabolism. Abnormal thyroid levels can cause fatigue, weight changes, and mood disturbances.\n\nWhat to do with your results:\n\nAlways review your results with your doctor rather than interpreting them in isolation. A single result slightly outside the reference range does not necessarily indicate disease — context matters. Our clinical team will explain what your results mean and what steps, if any, are needed.",
                Published = true,
                CreatedAt = new DateTime(2026, 3, 28, 9, 0, 0, DateTimeKind.Utc)
            },
            new Post
            {
                Title = "Maternal and Child Health: Supporting Families from the Start",
                Slug = "maternal-and-child-health-awareness",
                Summary = "Our Maternity Care department provides antenatal, delivery, and postnatal support for mothers and newborns. Here is what families can expect from our maternity services.",
                Content = "Good maternal and child health begins well before birth — and continues through the early years of a child's life. At Boniface & Paulina Okafor Memorial Hospital, our Maternity Care team is dedicated to supporting families through every stage of this journey.\n\nAntenatal Care:\n\nWe recommend that expectant mothers register for antenatal care as early as possible — ideally within the first 12 weeks of pregnancy. Early registration allows us to:\n- Confirm and date the pregnancy with an early ultrasound.\n- Screen for conditions such as gestational diabetes, anaemia, and infections.\n- Provide nutritional guidance and folic acid supplementation.\n- Monitor fetal growth and wellbeing throughout the pregnancy.\n\nOur antenatal clinic runs Monday, Wednesday, and Friday. New patients are seen by appointment.\n\nDelivery Services:\n\nOur maternity ward is staffed around the clock by qualified midwives and obstetricians. We offer:\n- Natural (vaginal) delivery with full midwifery support.\n- Planned and emergency caesarean sections.\n- Pain management options including gas and air.\n- Immediate skin-to-skin contact and breastfeeding support.\n\nPostnatal and Newborn Care:\n\nAfter delivery, mothers and newborns receive monitoring and support during their hospital stay. Our team provides breastfeeding guidance, newborn examination, and vaccination scheduling. Postnatal follow-up appointments are arranged before discharge.\n\nChild Health and Immunisation:\n\nOur paediatric team offers routine child health reviews and vaccination clinics for infants and children up to age 12. Immunisation protects children from serious preventable diseases — please ensure your child's vaccinations are up to date.\n\nTo register for antenatal care or to speak with our maternity team, please contact us at the hospital.",
                Published = true,
                CreatedAt = new DateTime(2026, 3, 15, 8, 0, 0, DateTimeKind.Utc)
            },
            // Draft post — not yet published, used to demonstrate draft workflow
            new Post
            {
                Title = "Upcoming Changes to Our Outpatient Clinic Hours (DRAFT)",
                Slug = "outpatient-clinic-hours-update-draft",
                Summary = "We are reviewing our outpatient scheduling to better serve patients. This post will be published once final timings are confirmed.",
                Content = "DRAFT — Do not publish until clinic hours are confirmed by administration.\n\nOur outpatient department is currently reviewing its operating hours to improve patient access and reduce peak-time waiting. We expect to publish confirmed new timings by the end of April 2026.\n\nPlease check back shortly for the final update.",
                Published = false,
                IsFeatured = false,
                CreatedAt = new DateTime(2026, 4, 22, 11, 0, 0, DateTimeKind.Utc)
            }
        };

        context.Posts.AddRange(posts);
        await context.SaveChangesAsync();
    }
}
