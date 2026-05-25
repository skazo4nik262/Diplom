-- 1. Таблица пользователей (оставляем или создаем, если нет)
CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    login VARCHAR(255) UNIQUE NOT NULL,
    password_hash TEXT NOT NULL,
    role INT DEFAULT 1,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW()
);



-- 5. Пользовательские данные (просмотры, оценки, избранное)
CREATE TABLE user_movies (
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    movie_id INT REFERENCES movies(id) ON DELETE CASCADE,
    status VARCHAR(20) DEFAULT 'none', -- 'none', 'planned', 'watched', 'dropped'
    rating INT CHECK (rating >= 1 AND rating <= 10), -- Оценка пользователя
    watched_at TIMESTAMPTZ, -- Дата просмотра
    created_at TIMESTAMPTZ DEFAULT NOW(),
    PRIMARY KEY (user_id, movie_id)
);