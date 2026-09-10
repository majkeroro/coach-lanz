/* Booking Suite Engine — config + store + availability + bookings.
   NOTE (full-stack): when window.SERVER_CONFIG is present, api-client.js
   overrides the data methods below with server-backed versions.
   This file remains as the offline/static-demo fallback. */
(function(){
  const LS_BOOK = "bs_bookings_v1";
  const LS_OVER = "bs_config_override_v1";

  function deepMerge(a,b){
    if(!b) return a;
    const out = Array.isArray(a) ? [...a] : {...a};
    for(const k of Object.keys(b)){
      if(b[k] && typeof b[k]==="object" && !Array.isArray(b[k]) && a && typeof a[k]==="object" && !Array.isArray(a[k])){
        out[k]=deepMerge(a[k],b[k]);
      } else out[k]=b[k];
    }
    return out;
  }
  function getOverride(){ try{ return JSON.parse(localStorage.getItem(LS_OVER)||"{}"); }catch{ return {}; } }
  function saveOverride(patch){
    const cur = getOverride();
    const merged = deepMerge(cur, patch);
    localStorage.setItem(LS_OVER, JSON.stringify(merged));
    return merged;
  }
  function clearOverride(){ localStorage.removeItem(LS_OVER); }

  function getConfig(){
    const ov = getOverride();
    const base = window.BOOKING_DEFAULTS;
    // services come from preset type unless overridden
    const type = (ov.business && ov.business.type) || base.business.type || "fitness";
    const preset = (window.BOOKING_PRESETS && window.BOOKING_PRESETS[type]) ? window.BOOKING_PRESETS[type] : window.BOOKING_PRESETS.fitness;
    let cfg = JSON.parse(JSON.stringify(base));
    // apply preset defaults for business name/color + services/categories if not customized
    if(!ov.services) { cfg._presetServices = preset.services; cfg._categories = preset.categories; }
    if(!ov.business || !ov.business.name) { /* keep base, but show preset label */ }
    // if type changed and no custom services, swap
    if(type !== base.business.type && !ov.services){
      cfg.business.name = preset.business.name;
      cfg.business.tagline = preset.business.tagline;
      cfg.business.color = preset.business.color;
      cfg.business.type = type;
    }
    cfg = deepMerge(cfg, ov);
    if(!cfg.services) cfg.services = preset.services;
    if(!cfg.categories) cfg.categories = preset.categories;
    if(!cfg.business.type) cfg.business.type = type;
    return cfg;
  }

  // ---- bookings store ----
  function loadBookings(){
    try{
      const raw = JSON.parse(localStorage.getItem(LS_BOOK)||"[]");
      if(Array.isArray(raw) && raw.length) return raw;
    }catch{}
    // seed demo
    const seed = seedBookings();
    localStorage.setItem(LS_BOOK, JSON.stringify(seed));
    return seed;
  }
  function persist(b){ localStorage.setItem(LS_BOOK, JSON.stringify(b)); }
  function seedBookings(){
    const today = new Date(); today.setHours(0,0,0,0);
    const iso = d => d.toISOString().slice(0,10);
    const d1 = new Date(today); d1.setDate(d1.getDate()+1);
    const d2 = new Date(today); d2.setDate(d2.getDate()+2);
    return [
      { ref:"BK-DEMO1", serviceId:"fit-pt", serviceName:"1-on-1 Personal Training", staffId:"lanz", staffName:"Coach Lanz", locationId:"main", locationName:"Main Branch (Cavite)", date:iso(d1), time:"09:00", duration:60, price:800, addons:[], coupon:"", total:800, name:"Juan Dela Cruz", phone:"09171234567", email:"juan@mail.com", notes:"Fat loss goal", pay:"Cash", status:"confirmed", createdAt:Date.now()-86400000 },
      { ref:"BK-DEMO2", serviceId:"fit-mob", serviceName:"Mobility & Recovery Session", staffId:"mia", staffName:"Mia R.", locationId:"main", locationName:"Main Branch (Cavite)", date:iso(d2), time:"14:00", duration:45, price:600, addons:[], coupon:"", total:600, name:"Maria S.", phone:"09179876543", email:"", notes:"Back stiffness", pay:"GCash", status:"pending", createdAt:Date.now()-3600000 }
    ];
  }

  function makeRef(){ return "BK-"+Math.random().toString(36).slice(2,7).toUpperCase(); }

  function createBooking(data){
    const all = loadBookings();
    const ref = makeRef();
    const rec = { ref, status:"confirmed", createdAt:Date.now(), ...data };
    all.push(rec); persist(all);
    return rec;
  }
  function updateBooking(ref, patch){
    const all = loadBookings();
    const i = all.findIndex(b=>b.ref.toLowerCase()===String(ref).toLowerCase());
    if(i<0) return null;
    all[i] = {...all[i], ...patch};
    persist(all); return all[i];
  }
  function deleteBooking(ref){
    let all = loadBookings();
    all = all.filter(b=>b.ref.toLowerCase()!==String(ref).toLowerCase());
    persist(all);
  }
  function findBookings(query){
    const all = loadBookings();
    const q = String(query||"").trim().toLowerCase();
    if(!q) return [];
    return all.filter(b=> b.ref.toLowerCase()===q || b.phone.replace(/\D/g,"").includes(q.replace(/\D/g,"")) || (b.name||"").toLowerCase().includes(q));
  }

  // ---- availability ----
  function toMin(t){ const [h,m]=t.split(":").map(Number); return h*60+m; }
  function toTime(min){ const h=String(Math.floor(min/60)).padStart(2,"0"); const m=String(min%60).padStart(2,"0"); return `${h}:${m}`; }

  function bookingsOn(date, staffId){
    return loadBookings().filter(b=> b.date===date && b.status!=="cancelled" && (staffId==="any" || b.staffId===staffId || b.staffId==="any" || staffId==="any" ? true : true));
    // Note: "any" blocks conservatively — filtered properly in slot check
  }

  function generateSlots(dateStr, serviceId, staffId){
    const cfg = getConfig();
    const svc = cfg.services.find(s=>s.id===serviceId);
    if(!svc) return [];
    const dur = svc.duration + (cfg.business.buffer||0);
    const d = new Date(dateStr+"T12:00:00");
    if(isNaN(d)) return [];
    const dow = String(d.getDay());
    const hrs = cfg.hours[dow];
    if(!hrs) return []; // closed
    if((cfg.blockedDates||[]).includes(dateStr)) return [];
    // past dates
    const todayStr = new Date().toISOString().slice(0,10);
    if(dateStr < todayStr) return [];
    const interval = cfg.business.slotInterval || 30;
    const open = toMin(hrs.open), close = toMin(hrs.close);
    const bs = hrs.breakStart?toMin(hrs.breakStart):null, be = hrs.breakEnd?toMin(hrs.breakEnd):null;
    const existing = loadBookings().filter(b=>b.date===dateStr && b.status!=="cancelled");
    const slots = [];
    for(let t=open; t+svc.duration<=close; t+=interval){
      const end = t+svc.duration;
      if(bs!==null && t<be && end>bs) continue; // overlaps break
      // conflict check: if specific staff, only that staff's bookings block; if "any", need at least one capable staff free
      const capable = cfg.staff.filter(s=> s.id!=="any" && (s.services==="all" || (s.services||[]).includes(serviceId)));
      const checkOverlap = (sid) => existing.filter(b=> b.staffId===sid || b.staffId==="any").some(b=>{
        const bt = toMin(b.time); const bend = bt + (b.duration||60);
        // new booking occupies [t, end+buffer)
        return t < bend && (end + (cfg.business.buffer||0)) > bt;
      });
      let available = false, assignStaff = staffId;
      if(staffId && staffId!=="any"){
        available = !checkOverlap(staffId);
      } else {
        // any capable staff free?
        const free = capable.find(s=>!checkOverlap(s.id));
        available = !!free;
        if(free) assignStaff = free.id;
      }
      // same-day: skip past times
      if(dateStr===todayStr){
        const now = new Date(); const nowMin = now.getHours()*60+now.getMinutes()+30;
        if(t < nowMin) continue;
      }
      slots.push({ time: toTime(t), available, assignStaff });
    }
    return slots;
  }

  function priceCalc(serviceId, addonIds, couponCode){
    const cfg = getConfig();
    const svc = cfg.services.find(s=>s.id===serviceId);
    const base = svc?svc.price:0;
    const addons = (cfg.addons||[]).filter(a=>(addonIds||[]).includes(a.id));
    const addTotal = addons.reduce((s,a)=>s+a.price,0);
    let total = base+addTotal, discount=0;
    const cp = (cfg.coupons||[]).find(c=>c.code.toLowerCase()===String(couponCode||"").trim().toLowerCase());
    if(cp && total>0){
      if(cp.type==="percent") discount = Math.round(total*cp.value/100);
      else discount = Math.min(cp.value,total);
      // FREEASSESS style minimum
      if(cp.code==="FREEASSESS" && total<1500) discount=0;
      total = total-discount;
    }
    return { base, addTotal, discount, total, coupon: discount?cp:null };
  }

  function stats(){
    const all = loadBookings();
    const todayStr = new Date().toISOString().slice(0,10);
    const today = all.filter(b=>b.date===todayStr && b.status!=="cancelled");
    const upcoming = all.filter(b=>b.date>=todayStr && b.status!=="cancelled").sort((a,b)=>(a.date+a.time).localeCompare(b.date+b.time));
    const revenue = all.filter(b=>b.status!=="cancelled").reduce((s,b)=>s+(b.total||0),0);
    return { total: all.length, today: today.length, upcoming: upcoming.slice(0,8), revenue, all };
  }

  function icsFor(b){
    const dt = b.date.replace(/-/g,"")+"T"+b.time.replace(":","")+"00";
    const endMin = toMin(b.time)+(b.duration||60);
    const end = b.date.replace(/-/g,"")+"T"+toTime(endMin).replace(":","")+"00";
    return ["BEGIN:VCALENDAR","VERSION:2.0","BEGIN:VEVENT","UID:"+b.ref+"@bookingsuite","DTSTAMP:"+dt,"DTSTART:"+dt,"DTEND:"+end,"SUMMARY:"+b.serviceName+" ("+b.ref+")","DESCRIPTION:Staff: "+b.staffName+"\\nLocation: "+(b.locationName||"")+"\\nRef: "+b.ref,"LOCATION:"+(b.locationName||""),"END:VEVENT","END:VCALENDAR"].join("\r\n");
  }

  window.BS = { getConfig, getOverride, saveOverride, clearOverride, loadBookings, persist, createBooking, updateBooking, deleteBooking, findBookings, generateSlots, priceCalc, stats, icsFor, deepMerge };
})();
