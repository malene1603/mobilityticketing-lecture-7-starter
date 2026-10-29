-- Design persistent storage for published dispatch plans in lab07.
-- Include the plan identity, time window, publication time and displayed values.
-- The contract defines behavior; the table layout is your choice.


-- Published dispatch plan header.
CREATE TABLE lab07.published_plans (
   id text PRIMARY KEY,
   from_utc timestamptz NOT NULL,
   to_utc timestamptz NOT NULL,
   published_at_utc timestamptz NOT NULL,

   CONSTRAINT published_plans_valid_window
       CHECK (
           from_utc < to_utc
               AND to_utc <= from_utc + INTERVAL '24 hours'
)
    );

-- Immutable copy of the values displayed for each route.
CREATE TABLE lab07.published_plan_routes (
     plan_id text NOT NULL
         REFERENCES lab07.published_plans(id)
             ON DELETE CASCADE,
     route_id text NOT NULL,
     route_name text NOT NULL,
     scheduled_trip_count integer NOT NULL
         CHECK (scheduled_trip_count >= 0),
     next_departure_utc timestamptz NULL,
    
     PRIMARY KEY (plan_id, route_id)
);

