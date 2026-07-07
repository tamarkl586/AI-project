# ADR-001: אסטרטגיית Polyglot Persistence עבור מיקרו-שירותי שלב 2

- **סטטוס:** מאושר  
- **תאריך:** 2026-07-05  
- **סוג החלטה:** ארכיטקטורה / פלטפורמת נתונים  
- **היקף:** שכבת הנתונים בפרודקשן עבור CatalogService, IdentityService, TicketingService, DrawReportService

## הקשר

בשלב 2 של המיגרציה פורק המונולית למיקרו-שירותים לפי Boundaries של Bounded Context, עם פרופילי עומס שונים ודרישות עקביות שונות. אימוץ פרדיגמת מסד נתונים יחידה לכלל השירותים היה כופה פשרות לא אופטימליות בין Latency SLOs, אילוצי שלמות נתונים ויעדי Throughput.

לכן אומצה ארכיטקטורת **Polyglot Persistence**, שבה לכל שירות נבחר מנוע הנתונים המתאים לסמנטיקה הדומיינית ולמאפייני העומס שלו. מסמך ADR זה מנסח את ההחלטה תוך שימוש במונחי מערכות מבוזרות:

- פשרות **CAP Theorem** תחת Network Partitions
- מודלי עקביות **ACID מול BASE**
- מאפייני **Read/Write Throughput**
- התאמת **Data Schema** (Relational, Document, Wide-Column)

---

## החלטה

### 1) CatalogService → MongoDB

#### החלטה
MongoDB נבחר כמסד הנתונים הראשי עבור דומיין הקטלוג ומתנות/מוצרים.

#### נימוק ארכיטקטוני
- **Data Schema:** לישויות הקטלוג יש מבנה דינמי מאוד (שדות אופציונליים, Metadata תלוי-קטגוריה, תכונות מוצר מתפתחות). **Document Schema** מאפשר פולימורפיזם בשדות ואבולוציית סכימה הדרגתית ללא עלות DDL גבוהה.
- **Read Throughput:** עומס הקטלוג מוטה קריאה (Browse/Search/Detail). Document Locality ואינדוקס במונגו מספקים Throughput קריאה גבוה וזמן תגובה נמוך לתצוגות מוצר אגרגטיביות.
- **CAP Theorem positioning:** בפריסת Replica Set, MongoDB מציג התנהגות **CP-leaning** בקריאות/כתיבות ל-Primary (עקביות חזקה יותר), לצד **AP-leaning** בקצוות קריאה (למשל Secondary Reads עם Eventual Consistency). גמישות זו מתאימה ל-UX של קטלוג, שבו Staleness נקודתי הוא נסבל.
- **Consistency model fit:** בדאטה של קטלוג ניתן לקבל Bounded Staleness טוב יותר מאשר בדומייני זהות או טיקטינג. לכן, מודל משולב של עקביות חזקה בכתיבות ו-Eventual Consistency בחלק מנתיבי הקריאה הוא בחירה תקינה.

---

### 2) IdentityService → PostgreSQL

#### החלטה
PostgreSQL נבחר כ-System of Record עבור משתמשים, אישורים, הרשאות ונתוני זהות.

#### נימוק ארכיטקטוני
- **ACID compliance:** דומיין הזהות הוא Integrity-Critical. תהליכי אימות והרשאה מחייבים נכונות מלאה: Atomicity, Isolation, Consistency, Durability.
- **Relational Schema:** נתוני זהות הם מטבעם רלציוניים (Users, Credentials, Roles/Claims, Tokens, Revocation Artifacts), עם Foreign Keys ואילוצי שלמות ברורים.
- **Immediate consistency:** מנגנוני הרשאה אינם יכולים להסתמך על מצב Credentials מיושן. דרושה עקביות מיידית כדי למנוע Authorization Drift או Privilege Escalation.
- **CAP Theorem positioning:** העדפה תפעולית ל-**CP** במצבי Partition; עקביות ותקינות אבטחתית מקבלות קדימות גם במחיר פגיעה זמנית בזמינות.

---

### 3) TicketingService → SQL Server

#### החלטה
SQL Server נבחר לניהול מלאי כרטיסים, מחזור חיי עגלה, הקצאה/שריון ותהליכי תשלום.

#### נימוק ארכיטקטוני
- **Business-critical transaction safety:** דומיין הטיקטינג והתשלום רגיש כספית ותפעולית ודורש הגנה מלאה מפני Double-Booking ו-Over-Allocation.
- **ACID + explicit transaction scopes:** נעשה שימוש ב-Transaction Scopes מפורשים, Locking ו-Concurrency Controls כדי להבטיח תוצאות עסקיות דטרמיניסטיות (למשל Reserve Seat → Capture Payment → Finalize Order).
- **Immediate consistency model:** מעברי מצב בעגלה ובתשלום חייבים עקביות מיידית כדי למנוע Phantom Availability, חיובים כפולים או מצב הזמנה לא עקבי.
- **Relational Schema:** סכימה רלציונית עם נרמול ואילוצים מתאימה לאכיפת חוקים עסקיים ולאודיטביליות מלאה של שינויי מצב.
- **CAP Theorem positioning:** בחירה **CP-oriented** לטובת Correctness First בתהליכי טרנזקציות קריטיות.

---

### 4) DrawReportService → Cassandra

#### החלטה
Apache Cassandra נבחר עבור לוגים של הגרלות, Audit Events ותוצרים אנליטיים מחושבים מראש.

#### נימוק ארכיטקטוני
- **Write throughput at scale:** דומיין זה מוטה כתיבה (Append-Heavy) ודורש קצב קליטה גבוה מאוד. Cassandra מותאם ל-Write Throughput מסיבי וסקייל אופקי.
- **Wide-column schema:** מודל Wide-Column מתאים ל-Time-Series Partitions, טבלאות דה-נורמליזציה מוכוונות שאילתה ונפחי אירועים גבוהים.
- **BASE consistency model:** לצרכי דיווח ואנליטיקה ניתן לאמץ Eventual Consistency; לא כל קריאה מחייבת עקביות מיידית.
- **Tunable consistency under CAP:** Cassandra מציג התנהגות **AP-leaning** תחת Partition, עם יכולת Consistency Tuning לכל פעולה (`ONE`, `QUORUM`, `ALL`) כדי לכייל Latency מול Consistency לפי Use Case.
- **Consistency fit:** Eventual Consistency מתאים למסכי דיווח שבהם חלון עדכניות מוגדר וסביר עסקית.

---

## השלכות

### השלכות חיוביות
- התאמה מיטבית בין מנוע נתונים לכל דומיין משפרת גם ביצועים וגם נכונות.
- צמצום Impedance Mismatch בין מודל עסקי למודל התמדה.
- שמירת עקביות חזקה היכן שנכונות אינה נתונה לפשרה (Identity, Ticketing).
- סקיילביליות קריאה גבוהה בקטלוג וסקיילביליות כתיבה גבוהה באנליטיקה ואודיט.
- הגדרת בעלות ברורה על Data Contracts ועל אבולוציית סכימה לכל Bounded Context.

### עלויות ופשרות
- מורכבות תפעולית גבוהה יותר (מספר מנועים, Backup/Restore, ניטור, Patch Cycles).
- עומס קוגניטיבי גדול יותר על צוותי הפיתוח (Query Models, אינדוקס, סמנטיקת עקביות).
- אינטגרציה בין שירותים מחייבת דפוסים מבוזרים (Events, CDC, Materialized Views) במקום Joins חוצי-מסד.
- צורך חזק יותר ב-Governance של חוזי נתונים ותאימות גרסאות.

### מנגנוני הפחתת סיכון
- סטנדרטיזציה של SLOs ו-Runbooks לכל מנוע נתונים.
- אכיפת Data Ownership ושיטת Versioning לסכימות.
- שימוש ב-Idempotent Consumers וב-Retry-Safe Writes.
- הגדרת Consistency SLA מפורש לכל API (Strong מול Eventual).
- תרגול DR ובדיקות שחזור תקופתיות לכל מנוע.

---

## סיכום החלטה

המערכת מאמצת ארכיטקטורת **Polyglot Persistence** מכוונת-דומיין:

- **CatalogService:** MongoDB עבור Document Schema גמיש ו-Read Throughput גבוה עם Bounded Staleness.
- **IdentityService:** PostgreSQL עבור ACID מלא ועקביות מיידית בגבולות הרשאה.
- **TicketingService:** SQL Server עבור טרנזקציות בטוחות ועקביות קשיחה בתהליכי הזמנה ותשלום.
- **DrawReportService:** Cassandra עבור קליטת כתיבה בהיקף גבוה ומודל BASE/עקבית-לבסוף עם Tunable Consistency.

החלטה זו מיישרת את בחירת הטכנולוגיה עם Invariants דומייניים, מאפייני Throughput, ודרישות CAP/Consistency הצפויות במערכות מבוזרות ברמת פרודקשן.
