/* ============================================================
   BOOKING SUITE — Business Config
   Any business can use this file. Edit BUSINESS + SERVICES +
   STAFF + HOURS, or do it visually in admin.html (no code).
   ============================================================ */
window.BOOKING_PRESETS = {
  fitness: {
    label: "Fitness / Coaching",
    business: { name: "Coach Lanz", tagline: "Fitness & Recovery Coaching", color: "#c8f04a" },
    categories: ["Training", "Recovery", "Online"],
    services: [
      { id: "fit-assess", name: "Free Assessment", category: "Training", duration: 30, price: 0, desc: "Goal review, movement screen + plan.", icon: "🎯" },
      { id: "fit-pt", name: "1-on-1 Personal Training", category: "Training", duration: 60, price: 800, desc: "Strength, fat-loss or striking fitness. Includes warm-up + cool-down.", icon: "💪" },
      { id: "fit-strike", name: "Muay Thai / Boxing Fitness", category: "Training", duration: 60, price: 900, desc: "Pads, bag rounds, footwork. No hard sparring.", icon: "🥊" },
      { id: "fit-mob", name: "Mobility & Recovery Session", category: "Recovery", duration: 45, price: 600, desc: "Hips, spine, shoulders. Desk-worker reset.", icon: "🧘" },
      { id: "fit-online", name: "Online Coaching Call", category: "Online", duration: 45, price: 700, desc: "Video coaching + program review.", icon: "💻" }
    ]
  },
  salon: {
    label: "Salon / Barbershop / Spa",
    business: { name: "Glow Studio", tagline: "Salon • Nails • Spa", color: "#f472b6" },
    categories: ["Hair", "Nails", "Spa"],
    services: [
      { id: "sal-cut", name: "Haircut + Styling", category: "Hair", duration: 45, price: 350, desc: "Consult, cut, wash & style.", icon: "💇" },
      { id: "sal-color", name: "Hair Color / Balayage", category: "Hair", duration: 120, price: 1800, desc: "Color + treatment + blowout.", icon: "🎨" },
      { id: "sal-mani", name: "Manicure + Gel", category: "Nails", duration: 60, price: 600, desc: "Shape, cuticle care + gel polish.", icon: "💅" },
      { id: "sal-mass", name: "Swedish Massage (60m)", category: "Spa", duration: 60, price: 900, desc: "Full-body relaxation massage.", icon: "💆" },
      { id: "sal-facial", name: "Deep Cleanse Facial", category: "Spa", duration: 75, price: 1200, desc: "Cleanse, exfoliate, mask + serum.", icon: "✨" }
    ]
  },
  clinic: {
    label: "Clinic / Dental / Vet",
    business: { name: "CarePlus Clinic", tagline: "Family • Dental • Wellness", color: "#38bdf8" },
    categories: ["General", "Dental", "Wellness"],
    services: [
      { id: "cli-gen", name: "General Consultation", category: "General", duration: 20, price: 500, desc: "Check-up + prescription.", icon: "🩺" },
      { id: "cli-dent", name: "Dental Cleaning", category: "Dental", duration: 45, price: 1200, desc: "Scaling, polish + oral exam.", icon: "🦷" },
      { id: "cli-lab", name: "Lab Test / Bloodwork", category: "General", duration: 15, price: 750, desc: "Fasting labs, walk-in friendly.", icon: "🧪" },
      { id: "cli-physio", name: "Physical Therapy", category: "Wellness", duration: 60, price: 1000, desc: "Rehab + mobility program.", icon: "🦵" },
      { id: "cli-vax", name: "Vaccination", category: "Wellness", duration: 15, price: 650, desc: "Flu / routine immunization.", icon: "💉" }
    ]
  },
  auto: {
    label: "Auto Shop / Home Services",
    business: { name: "FixRight Auto & Home", tagline: "Repair • Maintenance • Cleaning", color: "#fb923c" },
    categories: ["Auto", "Home"],
    services: [
      { id: "aut-oil", name: "Change Oil + Inspection", category: "Auto", duration: 60, price: 1500, desc: "Oil, filter + 21-point check.", icon: "🛢️" },
      { id: "aut-detail", name: "Full Car Detailing", category: "Auto", duration: 180, price: 3500, desc: "Interior + exterior detail.", icon: "🚗" },
      { id: "aut-ac", name: "Aircon Cleaning", category: "Home", duration: 90, price: 1800, desc: "Split-type deep clean.", icon: "❄️" },
      { id: "aut-clean", name: "Deep Home Cleaning", category: "Home", duration: 180, price: 2800, desc: "3BR home, team of 2.", icon: "🧹" }
    ]
  },
  consult: {
    label: "Consultancy / Tutoring / Photography",
    business: { name: "ProAdvice Co.", tagline: "Consult • Tutor • Create", color: "#a78bfa" },
    categories: ["Business", "Lessons", "Creative"],
    services: [
      { id: "con-disc", name: "Discovery Call (Free)", category: "Business", duration: 20, price: 0, desc: "Fit check + quote.", icon: "📞" },
      { id: "con-strat", name: "1-hr Strategy Session", category: "Business", duration: 60, price: 2000, desc: "Roadmap + action plan.", icon: "📊" },
      { id: "con-tutor", name: "Tutoring (Math/English)", category: "Lessons", duration: 60, price: 600, desc: "1-on-1, all levels.", icon: "📚" },
      { id: "con-photo", name: "Portrait Shoot", category: "Creative", duration: 90, price: 4500, desc: "30 edited photos + prints.", icon: "📸" }
    ]
  }
};

window.BOOKING_DEFAULTS = {
  business: {
    name: "Coach Lanz",
    tagline: "Fitness & Recovery Coaching",
    phone: "0917-000-0000",
    messenger: "https://www.facebook.com/profile.php?id=61590590284675",
    address: "Cavite, Philippines • Online worldwide",
    currency: "₱",
    color: "#c8f04a",
    type: "fitness",
    slotInterval: 30,
    buffer: 10,
    advanceDays: 30,
    cancelHours: 12
  },
  locations: [
    { id: "main", name: "Main Branch (Cavite)" },
    { id: "online", name: "Online / Video Call" }
  ],
  staff: [
    { id: "any", name: "First Available", role: "Any specialist", avatar: "✨", services: "all" },
    { id: "lanz", name: "Coach Lanz", role: "Head Coach", avatar: "🥊", services: ["fit-assess","fit-pt","fit-strike","fit-mob","fit-online"] },
    { id: "mia", name: "Mia R.", role: "Mobility & Recovery", avatar: "🧘", services: ["fit-mob","fit-assess","fit-pt"] },
    { id: "jv", name: "JV D.", role: "Strength & Conditioning", avatar: "💪", services: ["fit-pt","fit-assess","fit-online"] }
  ],
  hours: {
    "0": null,
    "1": { open: "08:00", close: "19:00", breakStart: "12:00", breakEnd: "13:00" },
    "2": { open: "08:00", close: "19:00", breakStart: "12:00", breakEnd: "13:00" },
    "3": { open: "08:00", close: "19:00", breakStart: "12:00", breakEnd: "13:00" },
    "4": { open: "08:00", close: "19:00", breakStart: "12:00", breakEnd: "13:00" },
    "5": { open: "08:00", close: "19:00", breakStart: "12:00", breakEnd: "13:00" },
    "6": { open: "09:00", close: "17:00", breakStart: null, breakEnd: null }
  },
  blockedDates: [],
  addons: [
    { id: "ad-early", name: "Early-bird / Priority slot", price: 150 },
    { id: "ad-plan", name: "Printed program / aftercare kit", price: 250 },
    { id: "ad-video", name: "Video form review", price: 200 }
  ],
  coupons: [
    { code: "WELCOME10", type: "percent", value: 10, label: "10% off first booking" },
    { code: "FREEASSESS", type: "fixed", value: 500, label: "₱500 off ₱1500+" }
  ],
  policies: {
    cancel: "Free cancellation up to 12h before. Inside 12h, please message us to reschedule.",
    noshow: "No-show = forfeited slot. Message us to rebook.",
    payment: "Pay via Cash, GCash, or Card on arrival / online. No prepayment required unless stated."
  }
};
