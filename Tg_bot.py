import asyncio
import pymysql
from aiogram import Bot, Dispatcher, types
from aiogram.filters import Command
from aiogram.types import ReplyKeyboardMarkup, KeyboardButton, Message

#Токен бота
API_TOKEN = "7888653030:AAGOA9cd2A_jOfZYefaB7GBjBmEjIZ4S0Cs"

DB_HOST = "localhost"
DB_USER = "root"
DB_PASSWORD = "1111"
DB_NAME = "college_schedule"

bot = Bot(token=API_TOKEN)
dp = Dispatcher()

user_state = {}
days_kb = ReplyKeyboardMarkup(
    keyboard=[
        [KeyboardButton(text="Понедельник"), KeyboardButton(text="Вторник")],
        [KeyboardButton(text="Среда"), KeyboardButton(text="Четверг")],
        [KeyboardButton(text="Пятница"), KeyboardButton(text="Суббота")],
        [KeyboardButton(text="🔙 Назад")]
    ],
    resize_keyboard=True
)

def get_connection():
    return pymysql.connect(
        host=DB_HOST,
        user=DB_USER,
        password=DB_PASSWORD,
        database=DB_NAME,
        cursorclass=pymysql.cursors.DictCursor
    )

def get_groups():
    conn = get_connection()
    try:
        with conn.cursor() as cursor:
            cursor.execute("SELECT DISTINCT name FROM student_groups WHERE name != ''")
            return [row["name"] for row in cursor.fetchall()]
    finally:
        conn.close()

def get_courses_for_group(group_name):
    conn = get_connection()
    try:
        with conn.cursor() as cursor:
            cursor.execute("SELECT DISTINCT course FROM student_groups WHERE name = %s ORDER BY course", (group_name,))
            return [row["course"] for row in cursor.fetchall()]
    finally:
        conn.close()

# ==================== ОБРАБОТЧИКИ ====================

@dp.message(Command("start"))
async def cmd_start(message: Message):
    await show_group_selection(message)


async def show_group_selection(message: Message):
    """Показать выбор групп (самое начало)"""
    user_id = message.from_user.id
    if user_id in user_state:
        del user_state[user_id] 

    groups = get_groups()
    if not groups:
        await message.answer("❌ В базе нет групп.")
        return

    kb = ReplyKeyboardMarkup(
        keyboard=[[KeyboardButton(text=g)] for g in groups],
        resize_keyboard=True
    )

    await message.answer(
        "👋 Привет! Выбери свою группу:",
        reply_markup=kb
    )


@dp.message()
async def choose_group_course_or_day(message: Message):
    user_id = message.from_user.id
    text = message.text.strip()

    # ==================== КНОПКА НАЗАД ====================
    if text == "🔙 Назад":
        await show_group_selection(message)
        return

    if user_id not in user_state:
        groups = get_groups()
        if text in groups:
            user_state[user_id] = {"group": text}
            courses = get_courses_for_group(text)

            kb = ReplyKeyboardMarkup(
                keyboard=[[KeyboardButton(text=str(c))] for c in courses] + [[KeyboardButton(text="🔙 Назад")]],
                resize_keyboard=True
            )

            await message.answer(
                f"✅ Группа <b>{text}</b> выбрана.\nТеперь выбери курс:",
                reply_markup=kb,
                parse_mode="HTML"
            )
        else:
            await message.answer("Пожалуйста, выбери группу из списка 👇")
        return

    if "course" not in user_state[user_id]:
        try:
            course = int(text)
        except ValueError:
            await message.answer("Выбери курс с кнопок ниже 👇")
            return

        user_state[user_id]["course"] = course
        
        await message.answer(
            f"📘 Курс {course} выбран.\nТеперь выбери день недели:",
            reply_markup=days_kb
        )
        return

    day_map = {
        "Понедельник": 1,
        "Вторник": 2,
        "Среда": 3,
        "Четверг": 4,
        "Пятница": 5,
        "Суббота": 6
    }

    if text not in day_map:
        await message.answer("Выбери день недели с кнопок ниже 👇")
        return

    group_name = user_state[user_id]["group"]
    course = user_state[user_id]["course"]
    day_of_week = day_map[text]

    try:
        conn = get_connection()
        with conn.cursor() as cursor:
            sql = """
                SELECT s.lesson_number, 
                       subj.name AS subject, 
                       t.full_name AS teacher, 
                       s.auditory, 
                       TIME_FORMAT(s.start_time, '%%H:%%i') as start,   
                       TIME_FORMAT(s.end_time, '%%H:%%i') as end
                FROM schedule s
                JOIN subjects subj ON s.subject_id = subj.id
                JOIN teachers t ON s.teacher_id = t.id
                JOIN student_groups g ON s.student_group_id = g.id
                WHERE g.name = %s AND g.course = %s AND s.day_of_week = %s
                ORDER BY s.lesson_number, s.start_time;
            """
            cursor.execute(sql, (group_name, course, day_of_week))
            rows = cursor.fetchall()

        if not rows:
            await message.answer(f"📅 На {text} занятий нет.")
            return

        reply = f"📅 Расписание для {group_name}, {course} курс на {text}:\n\n"
        for row in rows:
            reply += (f"{row['lesson_number']}. {row['subject']} \n"
                      f"👨‍🏫 {row['teacher']} \n"
                      f"🕑 {row['start']} - {row['end']} \n"
                      f"🏫 {row['auditory']}\n\n")

        await message.answer(reply)

    except Exception as e:
        await message.answer("Ошибка при получении расписания ❌")
        print("DB Error:", e)
    finally:
        conn.close()


async def main():
    print("Бот запущен...")
    await dp.start_polling(bot)


if __name__ == "__main__":
    asyncio.run(main())