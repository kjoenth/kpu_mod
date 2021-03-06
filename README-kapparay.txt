Kappa-Ray
=========

Kappa-Ray is a mod for the game Kerbal Space Program by Squad.
It subjects vessels to radiation depending on their location, and applies
 various effects of that radiation.

Kappa-Ray is currently in alpha test; not all features have been implemented.

Kappa-Ray is developed by Edward Cree, who goes by the handle 'soundnfury' on
 IRC and 'ec429' elsewhere; his website is at http://jttlov.no-ip.org.

Kappa-Ray is licensed under the GNU General Public License, version 2.


Radiation Effects
-----------------
Most parts will absorb part of the radiation passing through them, thus
 generating heat.  Parts with ModuleKappaRayAbsorber will be much more
 effective at this, depending on their absorpCoeff.  (This is useful for
 shielding).  Other parts' effective absorpCoeff increases with mass,
 asymptotically approaching 0.5.  (So for instance, a full fuel tank will
 absorb more than an empty one.)
Kerbals in command pods and cockpits may suffer radiation sickness, causing
 immediate death, or may develop cancer, causing delayed death.  Kerbals on
 EVA are at even greater risk, as they are completely unshielded.
Solar panels will suffer degradation of their photovoltaic cells, reducing
 their power output.

More effects are planned - see roadmap-kapparay for details...


Radiation Environments
----------------------
There are three types of kappa radiation:
* Van Allen.  Low energy ambient radiation, found in a belt around planets
   with magnetic fields.  (Currently only implemented for Kerbin).  Radiation
   of this type is also found (at very high fluxes) in the Sun's corona.
   The k-ray flux of Kerbin's Van Allen belts peaks at about 600km altitude
   over the equator.  At higher latitudes, the peak flux is lower, but so is
   the altitude at which it occurs.
   Most Van Allen kappa rays are at too low an energy to harm Kerbals, though
   exposure does create slight cancer risk.  However, even these low-energy
   rays are able to degrade solar panels, perhaps halving each panel's output
   in a year.
* Direct Solar.  Medium energy radiation emitted by the Sun.  As this is
   directional, it can be effectively shielded against: simply pointing vessel
   away from sun can drastically reduce kappa flux through command pods, as
   radiation is absorbed by engines, fuel tanks etc.  Another effective shield
   is a planetary magnetosphere, busily converting the solar kappa rays into
   lower-energy Van Allen radiation.  An atmosphere will block kappa rays even
   more thoroughly.  And of course, hiding in the dark behind a planet or moon
   will shield against direct solar radiation.
   The flux of solar k-radiation varies with the inverse square of distance to
   the Sun, which is bad news for any probes to Moho.  Also, the sun's k-rad
   output varies with time; in particular, every now and then a solar storm
   will occur (on average, one every 50 days or so).  These storms last for
   two days and, at their peak, k-rad flux may exceed fifty times its normal
   level!  Fortunately, they take some time to reach their peak, giving you
   time to make the appropriate preparations.
   Solar kappa rays are right in the energy level window most likely to cause
   cancer - good shielding is a must for any kerbals undergoing prolonged
   exposure to this environment.
* Galactic.  High-energy cosmic rays.  Like Van Allen radiation, galactic
   k-rads are ambient, but unlike Van Allen radiation, they hit very hard.
   Fortunately they have a much lower flux than other sources, and are only
   found once thoroughly outside the protection of planetary magnetic fields.
   Exposure to high-energy galactic radiation carries a risk of immediate
   radiation sickness and prompt death - any unshielded time in this
   environment is highly hazardous.  However, the low rate of this radiation
   means you may cheat the reaper for a while; but sooner or later your number
   will come up.


Known bugs:
-----------

Vessels other than the active one don't currently get radiation fired at them.
This is because turning that on causes the following bugs:
{
	Vessels out of physics range appear to fire their radiation at the active
	 vessel (their CoM is nonsense / in the wrong co-ordinate system).  This
	 is especially problematic because of asteroids, which are almost always
	 bathed in solar and galactic radiation.

	If there are multiple vessels in close proximity, each might get some of
	 the other's radiation.  Close proximity means about 10km.
	However, it's only _likely_ to take hits if it's much closer, as all
	 radiation is aimed within a 10m sphere of the CoM.
	This shouldn't be a problem, as vessels this close should have similar
	 radiation environments.  Also, the radiation isn't duplicated; if the
	 'wrong' vessel absorbs it, it won't then go on to hit the 'right' one.
	 So one vessel can shield another.
}

It's possible to have two instances of the FluxWindow open.

Kerbals don't actually die when they should; they lose their XP, and their
 camera shows K.I.A., but scene changes (?) resuscitate them.

Because of reasons, the OX-STAT fixed solar panel is no longer physicsless,
 which may unbalance some small probes with asymmetric OX-STATs.
