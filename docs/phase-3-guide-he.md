<div dir="rtl">

# מדריך שלב 3: ניהול תעבורה, BFF ואיזון עומסים

## 1) מה בוצע בפועל על 4 הסרוויסים שלך?

בשלב 3 יושמה שכבת תעבורה מסודרת מעל ארבעת הסרוויסים העסקיים שלך:

- IdentityService
- CatalogService
- TicketingService
- DrawReportService

### א. ApiGateway (YARP) כשער כניסה יחיד

המערכת עובדת במבנה של Single Public Entry Point:

- שער יחיד לצרכן חיצוני: ApiGateway
- טכנולוגיה: YARP
- פורט ציבורי: 8080

כל בקשה מהקליינט נכנסת קודם ל-Gateway, ורק ממנו מנותבת פנימה לסרוויס המתאים.

### ב. מדיניות בידוד רשת (הסתרת פורטים של סרוויסים)

ארבעת הסרוויסים העסקיים רצים כרכיבים פנימיים ברשת Docker בלבד:

- ללא מיפוי פורטים ישיר למכונה המארחת
- ללא גישה ישירה מהדפדפן/קליינט לכל סרוויס בנפרד
- גישה חיצונית רק דרך ה-Gateway

כך מתקבלת הפרדה נכונה בין שכבת חשיפה חיצונית לשכבת שירותים פנימית.

### ג. מנגנון Orchestration של WebBff

נוסף WebBff ייעודי לקליינט Web, שמבצע ריכוז נתונים (Aggregation) ומחזיר תשובה אחת נוחה לצריכה.

ה-BFF:

- מאמת JWT לפי חוזה האבטחה של IdentityService (Issuer, Audience, Signing Key)
- קורא ל-TicketingService כדי להביא נתוני עגלה/פריטי הזמנה של המשתמש
- קורא ל-CatalogService כדי להשלים פרטי מתנות
- ממזג את המידע ל-JSON אחד

נקודות קצה עיקריות:

- GET /api/web/orders/me
- GET /api/web/orders/{userId}

### ד. מבנה איזון עומסים (Load Balancing)

CatalogService רץ עם 2 רפליקות, וה-Gateway מבצע ביניהן חלוקת תעבורה בשיטת Round-Robin.

כדי להוכיח זאת בפועל, נוספה ב-CatalogService Middleware שמחזירה Header בשם:

- X-Container-ID

בקריאות חוזרות רואים ערכים שונים של X-Container-ID, כלומר הבקשות באמת מתחלקות בין קונטיינרים שונים.

---

## 2) פקודת ההרצה לבדיקת המערכת

```bash
docker compose up -d --build --scale catalogservice=2
```

הפקודה:

- בונה את הגרסה העדכנית
- מרימה את כל רכיבי המערכת
- מגדילה את CatalogService לשתי רפליקות עבור בדיקת איזון עומסים

---

## 3) תרחישי בדיקה (דמו למרצה)

### א. בדיקת איזון עומסים דרך X-Container-ID

```bash
for i in 1 2 3 4 5 6 7 8; do
  curl -s -D - http://localhost:8080/api/gift -o /dev/null | grep -i '^X-Container-ID:'
done
```

מה מצפים לראות:

- לפחות שני מזהי קונטיינר שונים שחוזרים לסירוגין.

### ב. בדיקת ה-BFF ללא הזדהות (Smoke Test)

```bash
curl -i http://localhost:8080/api/web/orders/me
```

מה מצפים לראות:

- סטטוס 401 Unauthorized.

### ג. בדיקת ה-BFF עם הזדהות מלאה (כולל טוקן פעיל)

שלב 1: קבלת טוקן התחברות.

```bash
TOKEN=$(curl -s -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"YOUR_USER_EMAIL","password":"YOUR_PASSWORD"}' \
  | jq -r '.token')

echo "$TOKEN"
```

שלב 2: קריאה ל-BFF עם Bearer Token.

```bash
curl -s -i http://localhost:8080/api/web/orders/me \
  -H "Authorization: Bearer $TOKEN"
```

מה מצפים לראות:

- סטטוס 200
- Payload מאוחד הכולל גם פריטי עגלה וגם פרטי מתנות מהקטלוג.

בדיקה משלימה לפי מזהה משתמש:

```bash
curl -s -i http://localhost:8080/api/web/orders/YOUR_USER_ID \
  -H "Authorization: Bearer $TOKEN"
```

- 200 אם זה המשתמש המחובר
- 403 אם מנסים לגשת ל-userId של משתמש אחר

---

## 4) הנחיית עבודה עם Cursor

כדי לעדכן את README הראשי בצורה בטוחה ואוטומטית:

1. פתחי את קובץ התיעוד באנגלית (Phase 3 Architecture Documentation and Prompt Guide).
2. העתקי את בלוק ההנחיה שבסעיף "Direct Cursor/Copilot Workspace Prompt".
3. הדביקי את הבלוק בצ'אט של Cursor בתוך ה-Workspace.
4. הריצי את הבקשה לעדכון README.
5. ודאי ידנית ששלב 1 ושלב 2 נשארו ללא מחיקה, ושנוסף רק סעיף Phase 3 עם:
   - ארכיטקטורת Gateway+BFF
   - פקודת scale
   - תרחישי בדיקה

כך מתקבל עדכון תיעוד עקבי, מהיר ובטוח בלי לפגוע באבני דרך קודמות.

</div>
