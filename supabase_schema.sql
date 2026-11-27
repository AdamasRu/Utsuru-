-- ============================================================================
-- UTSURU - SKEMA LENGKAP SUPABASE (POSTGRESQL + STORAGE + RLS)
-- Project: Utsuru Project (rmsqwdaspwqmowmanfed)
-- ============================================================================

-- 1. TABEL PROFIL PEMAIN
create table if not exists public.profiles (
    id uuid primary key references auth.users(id) on delete cascade,
    username text not null,
    email text not null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

-- 2. TABEL MASTER KOSAKATA & OBJEK (72 Entri)
create table if not exists public.master_vocabularies (
    id integer primary key,
    japanese_word text not null,
    kana text default '',
    romaji text not null,
    translation text not null,
    example_sentence text default '',
    category text default '',
    created_at timestamptz not null default now()
);

create table if not exists public.master_interactable_objects (
    object_id text primary key,
    object_name text not null,
    vocabulary_id integer not null references public.master_vocabularies(id) on delete cascade
);

create index if not exists idx_master_objects_vocab on public.master_interactable_objects(vocabulary_id);

-- 3. TABEL TOTAL POIN & SKOR PEMAIN
create table if not exists public.player_progress (
    user_id uuid primary key references public.profiles(id) on delete cascade,
    points integer not null default 0 check (points >= 0),
    total_words_discovered integer not null default 0,
    total_photos_taken integer not null default 0,
    updated_at timestamptz not null default now()
);

-- 4. TABEL LOG PENEMUAN KATA & OBJEK (KAMUS)
create table if not exists public.player_discovered_vocabularies (
    id bigint generated always as identity primary key,
    user_id uuid not null references public.profiles(id) on delete cascade,
    vocabulary_id integer not null references public.master_vocabularies(id) on delete cascade,
    discovered_at timestamptz not null default now(),
    constraint uq_player_vocab unique(user_id, vocabulary_id)
);

create index if not exists idx_player_discovered_vocab_user on public.player_discovered_vocabularies(user_id);

create table if not exists public.player_discovered_objects (
    id bigint generated always as identity primary key,
    user_id uuid not null references public.profiles(id) on delete cascade,
    object_id text not null references public.master_interactable_objects(object_id) on delete cascade,
    discovered_at timestamptz not null default now(),
    constraint uq_player_obj_user unique(user_id, object_id)
);

create index if not exists idx_player_discovered_obj_user on public.player_discovered_objects(user_id);

-- 5. TABEL ALBUM & FOTO POLAROID
create table if not exists public.player_albums (
    id uuid primary key default gen_random_uuid(),
    user_id uuid not null references public.profiles(id) on delete cascade,
    local_album_id integer not null,
    album_name text not null default 'Album Utama',
    is_default boolean not null default false,
    created_at timestamptz not null default now(),
    constraint uq_player_local_album unique(user_id, local_album_id)
);

create index if not exists idx_player_albums_user on public.player_albums(user_id);

create table if not exists public.player_photos (
    photo_id text primary key,
    user_id uuid not null references public.profiles(id) on delete cascade,
    album_id uuid references public.player_albums(id) on delete set null,
    vocabulary_id integer references public.master_vocabularies(id) on delete set null,
    object_id text references public.master_interactable_objects(object_id) on delete set null,
    object_name text not null,
    storage_path text not null,
    date_taken timestamptz not null default now(),
    is_favorite boolean not null default false,
    is_liked boolean not null default false,
    created_at timestamptz not null default now()
);

create index if not exists idx_player_photos_user on public.player_photos(user_id);
create index if not exists idx_player_photos_album on public.player_photos(album_id);
create index if not exists idx_player_photos_vocab on public.player_photos(vocabulary_id);

-- 5b. TABEL KEPEMILIKAN & TEMA AKTIF KAMUS (STORE)
create table if not exists public.player_themes (
    id bigint generated always as identity primary key,
    user_id uuid not null references public.profiles(id) on delete cascade,
    theme_id text not null,
    is_applied boolean not null default false,
    purchased_at timestamptz not null default now(),
    constraint uq_player_theme unique(user_id, theme_id)
);

create index if not exists idx_player_themes_user on public.player_themes(user_id);

-- 6. KEAMANAN ROW LEVEL SECURITY (RLS)
alter table public.profiles enable row level security;
alter table public.master_vocabularies enable row level security;
alter table public.master_interactable_objects enable row level security;
alter table public.player_progress enable row level security;
alter table public.player_discovered_vocabularies enable row level security;
alter table public.player_discovered_objects enable row level security;
alter table public.player_albums enable row level security;
alter table public.player_photos enable row level security;
alter table public.player_themes enable row level security;

-- 7. RLS POLICIES (Hak Akses Data)
create policy "Allow read master vocabularies" on public.master_vocabularies for select to public using (true);
create policy "Allow read master objects" on public.master_interactable_objects for select to public using (true);

create policy "Users can view own profile" on public.profiles for select to authenticated using ((select auth.uid()) = id);
create policy "Users can update own profile" on public.profiles for update to authenticated using ((select auth.uid()) = id) with check ((select auth.uid()) = id);
create policy "Users can insert own profile" on public.profiles for insert to authenticated with check ((select auth.uid()) = id);

create policy "Users can view own progress" on public.player_progress for select to authenticated using ((select auth.uid()) = user_id);
create policy "Users can update own progress" on public.player_progress for update to authenticated using ((select auth.uid()) = user_id) with check ((select auth.uid()) = user_id);
create policy "Users can insert own progress" on public.player_progress for insert to authenticated with check ((select auth.uid()) = user_id);

create policy "Users can view own discovered vocab" on public.player_discovered_vocabularies for select to authenticated using ((select auth.uid()) = user_id);
create policy "Users can insert own discovered vocab" on public.player_discovered_vocabularies for insert to authenticated with check ((select auth.uid()) = user_id);

create policy "Users can view own discovered objects" on public.player_discovered_objects for select to authenticated using ((select auth.uid()) = user_id);
create policy "Users can insert own discovered objects" on public.player_discovered_objects for insert to authenticated with check ((select auth.uid()) = user_id);

create policy "Users can view own albums" on public.player_albums for select to authenticated using ((select auth.uid()) = user_id);
create policy "Users can insert own albums" on public.player_albums for insert to authenticated with check ((select auth.uid()) = user_id);
create policy "Users can update own albums" on public.player_albums for update to authenticated using ((select auth.uid()) = user_id) with check ((select auth.uid()) = user_id);
create policy "Users can delete own albums" on public.player_albums for delete to authenticated using ((select auth.uid()) = user_id);

create policy "Users can view own photos" on public.player_photos for select to authenticated using ((select auth.uid()) = user_id);
create policy "Users can insert own photos" on public.player_photos for insert to authenticated with check ((select auth.uid()) = user_id);
create policy "Users can update own photos" on public.player_photos for update to authenticated using ((select auth.uid()) = user_id) with check ((select auth.uid()) = user_id);
create policy "Users can delete own photos" on public.player_photos for delete to authenticated using ((select auth.uid()) = user_id);

create policy "Users can view own themes" on public.player_themes for select to authenticated using ((select auth.uid()) = user_id);
create policy "Users can insert own themes" on public.player_themes for insert to authenticated with check ((select auth.uid()) = user_id);
create policy "Users can update own themes" on public.player_themes for update to authenticated using ((select auth.uid()) = user_id) with check ((select auth.uid()) = user_id);
create policy "Users can delete own themes" on public.player_themes for delete to authenticated using ((select auth.uid()) = user_id);

-- 8. TRIGGER OTOMATIS: INITIALIZE DATA SAAT USER REGISTER DI AUTH.USERS
-- Jalankan blok ini di SQL Editor Dashboard Supabase jika trigger belum aktif:
create or replace function public.handle_new_user()
returns trigger
language plpgsql
security definer set search_path = ''
as $$
declare
  v_username text;
begin
  v_username := coalesce(new.raw_user_meta_data->>'username', split_part(new.email, '@', 1));

  insert into public.profiles (id, username, email)
  values (new.id, v_username, new.email)
  on conflict (id) do update set
    email = excluded.email,
    updated_at = now();

  insert into public.player_progress (user_id, points)
  values (new.id, 0)
  on conflict (user_id) do nothing;

  insert into public.player_albums (user_id, local_album_id, album_name, is_default)
  values (new.id, 1, 'Album Utama', true)
  on conflict (user_id, local_album_id) do nothing;

  insert into public.player_themes (user_id, theme_id, is_applied)
  values (new.id, 'plain', true)
  on conflict (user_id, theme_id) do nothing;

  return new;
end;
$$;

drop trigger if exists on_auth_user_created on auth.users;
create trigger on_auth_user_created
  after insert on auth.users
  for each row execute procedure public.handle_new_user();

-- 9. PERIZINAN SCHEMA PUBLIC
grant usage on schema public to anon, authenticated;
grant all on all tables in schema public to anon, authenticated;
grant all on all sequences in schema public to anon, authenticated;
