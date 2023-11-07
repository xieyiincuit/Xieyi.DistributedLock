local function Unlock(key, field)
    if (redis.call('HEXISTS', key, field) == 0) then
        return 0
    end

    local counter = redis.call('HINCRBY', key, field, -1)

    if (counter < 1) then
        redis.call('DEL', key)
    end

    return 1
end

return Unlock(KEYS[1], ARGV[1])
