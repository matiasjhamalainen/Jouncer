using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Effects;
using Jypeli.Widgets;

/// @authors laolkorh, majuhama
/// @version 9.12.2015

public class Jouncer : PhysicsGame
{
    private Image taustaKuva = LoadImage("Tausta2"); //taustakuvan asetus
    private Image taustaValikko = LoadImage("Jouncer2");
    private Image pelaajaHahmo = LoadImage("pelaaja"); //pelaajan kuvan asetus
    private Image[] pelimerkit = LoadImages("bling", "Coin", "dollari", "moneysack");
    private Image[] laatat = LoadImages("Laatta", "Laatta2");
    private Image alasinKuva = LoadImage("anvil");
    private Image vReuna = LoadImage("SeinäL");
    private Image rReuna = LoadImage("SeinäR");

    private SoundEffect hyppy = LoadSoundEffect("BOUNCE01");
    private SoundEffect raha = LoadSoundEffect("cash");

    private List<Label> valikonKohdat;
    private PlatformCharacter player;
    private PhysicsObject suorakulmio;
    private IntMeter pelaajanPisteet;
    private PhysicsObject alaReuna;

    private EasyHighScore topLista = new EasyHighScore();

    public override void Begin()
    {
        Level.Background.Image = taustaKuva; //peli käyttää taustakuvaa

        Mouse.IsCursorVisible = true;

        Valikko();


    }


    /// <summary>
    /// Luodaan peliin alkuvalikko.
    /// </summary>
    private void Valikko()
    {
        ClearAll(); // Tyhjennetään kenttä kaikista peliolioista
        Level.Background.Image = taustaValikko;
        MediaPlayer.Stop();
        MediaPlayer.Play("DeserveToBeLoved");

        valikonKohdat = new List<Label>(); // Alustetaan lista, johon valikon kohdat tulevat

        Label kohta1 = new Label("Aloita uusi peli");  // Luodaan uusi Label-olio, joka toimii uuden pelin aloituskohtana
        kohta1.Position = new Vector(0, 40);  // Asetetaan valikon ensimmäinen kohta hieman kentän keskikohdan yläpuolelle
        valikonKohdat.Add(kohta1);  // Lisätään luotu valikon kohta listaan jossa kohtia säilytetään

        Label kohta2 = new Label("Parhaat pisteet");
        kohta2.Position = new Vector(0, 0);
        valikonKohdat.Add(kohta2);

        Label kohta3 = new Label("Lopeta peli");
        kohta3.Position = new Vector(0, -40);
        valikonKohdat.Add(kohta3);

        // Lisätään kaikki luodut kohdat peliin foreach-silmukalla
        foreach (Label valikonKohta in valikonKohdat)
        {
            Add(valikonKohta);
        }

        Mouse.ListenOn(kohta1, MouseButton.Left, ButtonState.Pressed, AloitaPeli, null);
        Mouse.ListenOn(kohta2, MouseButton.Left, ButtonState.Pressed, ParhaatPisteet, null);
        Mouse.ListenOn(kohta3, MouseButton.Left, ButtonState.Pressed, Exit, null);
        Mouse.ListenMovement(1.0, ValikossaLiikkuminen, null);
        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Näytä näppäinohjeet");

    }


    /// <summary>
    /// Aliohjelma seuraa hiiren liikkumista alkuvalikossa. Tekstin väri vaihtuu kursorin ollessa sen päällä.
    /// </summary>
    /// <param name="hiirenTila"></param>
    private void ValikossaLiikkuminen(AnalogState hiirenTila)
    {
        foreach (Label kohta in valikonKohdat)
        {
            if (Mouse.IsCursorOn(kohta))
            {
                kohta.TextColor = Color.Green;
            }
            else
            {
                kohta.TextColor = Color.Red;
            }

        }
    }


    /// <summary>
    /// Aliohjelma aloittaa uuden pelin kutsumalla tarvittavia aliohjelmia ja käynnistämällä laskurit.
    /// </summary>
    private void AloitaPeli()
    {
        ClearAll();
        Level.Background.Image = taustaKuva; //peli käyttää taustakuvaa
        MediaPlayer.Play("OneMustFall");
        MediaPlayer.IsRepeating = true;

        //KENTTÄ
        //SetWindowSize(1400, 1000);
        Level.Width = 900;
        Level.Height = 800;
        Gravity = new Vector(0, -600);
        Camera.ZoomToLevel();     // zoomi on normi

        lisaaSivut();
        Alkuasetelma();
        Pelaaja();
        AsetaOhjaimet();
        LisaaPistelaskuri();

        Timer t = new Timer();
        t.Interval = 2.22;
        t.Timeout += delegate { PiirraSuorakulmio(this, RandomGen.NextDouble(Level.Left + 150, Level.Right - 150), RandomGen.NextDouble(Level.Top - 50, Level.Top), RandomGen.NextDouble(130, 220)); };
        t.Start();

        Timer u = new Timer();
        u.Interval = 4.0;
        u.Timeout += delegate { Pelimerkit(this, RandomGen.NextDouble(Level.Left + 70, Level.Right - 70), RandomGen.NextDouble(Level.Top, Level.Top)); };
        u.Start();

        Timer v = new Timer();
        v.Interval = 10.0;
        v.Timeout += delegate { Alasimet(this, RandomGen.NextDouble(Level.Left + 100, Level.Right - 100), RandomGen.NextDouble(Level.Top, Level.Top)); };
        v.Start();

        AddCollisionHandler(player, "alasin", AlasinVsPelaaja);
        AddCollisionHandler(player, "pelimerkki", Merkinotto);
        AddCollisionHandler(alaReuna, "alasin", Putoaminen);
        AddCollisionHandler(alaReuna, "pelimerkki", Putoaminen);
        AddCollisionHandler(alaReuna, player, PelaajanPutoaminen);
        AddCollisionHandler(alaReuna, "laatta", Putoaminen);
    }


    /// <summary>
    /// Aliohjelma näyttää top-listan.
    /// </summary>
    private void ParhaatPisteet()
    {
        topLista.Show();
    }
    

    /// <summary>
    /// Aliohjelma asettaa ohjaimet pelaajan liikkumiseen.
    /// </summary>
    private void AsetaOhjaimet()
    {
        Keyboard.Listen(Key.Left, ButtonState.Down, Kavelevasen, "Liikuta hamoa vasemmalle", player);
        Keyboard.Listen(Key.Right, ButtonState.Down, Kaveleoikea, "Liikuta hahmoa oikealle", player);
        Keyboard.Listen(Key.Up, ButtonState.Pressed, Hyppaa, "Hyppää ylös", player);

        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Näytä näppäinohjeet");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
    }


    /// <summary>
    /// Aliohjelma toteuttaa pelaajan ylöspäin hypyn.
    /// </summary>
    /// <param name="player"></param>
    private void Hyppaa(PlatformCharacter player)
    {
        player.Jump(800);
        hyppy.Play();
    }


    /// <summary>
    /// Aliohjelma toteuttaa pelaajan kävelyn oikealle.
    /// </summary>
    /// <param name="player"></param>
    private void Kaveleoikea(PlatformCharacter player)
    {
        player.Walk(300);
        player.FacingDirection = Direction.Left;
    }


    /// <summary>
    /// Aliohjelma toteuttaa pelaajan kävelyn vasemmalle.
    /// </summary>
    /// <param name="player"></param>
    private void Kavelevasen(PlatformCharacter player)
    {
        player.Walk(-300);
        player.FacingDirection = Direction.Right;
    }


    /// <summary>
    /// Aliohjelma lisää liikuteltavan pelaajan hahmon peliin.
    /// </summary>
    private void Pelaaja()
    {
        player = new PlatformCharacter(35, 35, Shape.Circle);
        player.Y = 310;
        player.Restitution = 0.0;
        player.Image = pelaajaHahmo;
        player.CanRotate = false;
        player.CanMoveOnAir = true;
        Add(player);
    }


    /// <summary>
    /// Aliohjelma lisää peliin reunat ylälaitaan sekä vasempaan ja oikeaan laitaan.
    /// </summary>
    private void lisaaSivut()
    {
        alaReuna = Level.CreateBottomBorder(); 
        alaReuna.Restitution = 0.0;
        alaReuna.IsVisible = false;
        alaReuna.KineticFriction = 0.0;
        Add(alaReuna);


        PhysicsObject ylaReuna = Surface.CreateTop(Level);
        ylaReuna.Restitution = 0.0;
        ylaReuna.IsVisible = false;
        ylaReuna.KineticFriction = 0.0;
        Add(ylaReuna);

        PhysicsObject vasenReuna = Level.CreateLeftBorder();
        vasenReuna.KineticFriction = 0.0;
        vasenReuna.Restitution = 0.1;
        vasenReuna.Height = 400;
        vasenReuna.Width = 800;
        vasenReuna.Image = vReuna;
        vasenReuna.X = -600;
        Add(vasenReuna);

        PhysicsObject oikeaReuna = Surface.CreateRight(Level);
        oikeaReuna.KineticFriction = 0.0;
        oikeaReuna.Restitution = 0.1;
        oikeaReuna.Height = 400;
        oikeaReuna.Width = 800;
        oikeaReuna.Image = rReuna;
        oikeaReuna.X = 600;
        Add(oikeaReuna);

        vasenReuna.Tag = "sivu";
        oikeaReuna.Tag = "sivu";
    }


    /// <summary>
    /// Aliohjelma luo peliin laattojen alkuasetelman.
    /// </summary>
    private void Alkuasetelma()
    {
        PiirraSuorakulmio(this, -300, 300, 160);
        PiirraSuorakulmio(this, 0, 200, 160);
        PiirraSuorakulmio(this, 200, 150, 160);
        PiirraSuorakulmio(this, 300, 0, 160);
        PiirraSuorakulmio(this, 0, -100, 160);
        PiirraSuorakulmio(this, -300, -300, 160);
    }


    /// <summary>
    /// Aliohjelma lisää peliin putoavan laatan.
    /// </summary>
    /// <param name="peli"></param>
    /// <param name="x">Laatan sijainti x-akselilla.</param>
    /// <param name="y">Laatan sijainti y-akselilla.</param>
    /// <param name="z">Laatan leveys.</param>
    private void PiirraSuorakulmio(PhysicsGame peli, double x, double y, double z)
    {

        suorakulmio = new PhysicsObject(z, 15, Shape.Rectangle);
        suorakulmio.Position = new Vector(x, y);
        int kuva = RandomGen.NextInt(laatat.Length);
        suorakulmio.Image = laatat[kuva];
        suorakulmio.LinearDamping = 0.75;
        suorakulmio.Mass = 100000;
        peli.Add(suorakulmio);

        suorakulmio.Tag = "laatta";
        suorakulmio.CanRotate = false;
    }


    /// <summary>
    /// Aliohjelma palauttaa pelimerkin taulukosta.
    /// </summary>
    /// <param name="peli"></param>
    /// <param name="x">Pelimerkin sijainti x-akselilla.</param>
    /// <param name="y">Pelimerkin sijainti y-akselilla.</param>
    private void Pelimerkit(PhysicsGame peli, double x, double y)
    {
        PhysicsObject pelimerkki = new PhysicsObject(35, 25, Shape.Rectangle);
        pelimerkki.Position = new Vector(x, y);
        pelimerkki.IgnoresCollisionResponse = true;
        peli.Add(pelimerkki);

        pelimerkki.LinearDamping = 0.9;
        pelimerkki.Tag = "pelimerkki";
        int kuva = RandomGen.NextInt(pelimerkit.Length);
        pelimerkki.Image = pelimerkit[kuva];
    }


    /// <summary>
    /// Aliohjelma luo peliin tippuvan alasimen
    /// </summary>
    /// <param name="peli"></param>
    /// <param name="x">Alasimen sijainti x-akselilla.</param>
    /// <param name="y">Alasimen sijainti y-akselilla.</param>
    private void Alasimet(PhysicsGame peli, double x, double y)
    {
        PhysicsObject alasin = new PhysicsObject(100, 100, Shape.Rectangle);
        alasin.Position = new Vector(x, y);
        alasin.IgnoresCollisionResponse = true;
        peli.Add(alasin);

        alasin.LinearDamping = 1.0;
        alasin.Image = alasinKuva;
        alasin.Tag = "alasin";
    }


    /// <summary>
    /// Aliohjelma lisää pisteitä laskuriin, kun pelaaja osuu pelimerkkiin.
    /// </summary>
    /// <param name="pelaaja">Pelaajan hahmo.</param>
    /// <param name="pelimerkki">Tippuva pelimerkki.</param>
    private void Merkinotto(PhysicsObject pelaaja, PhysicsObject pelimerkki)
    {
        player = pelaaja as PlatformCharacter;
        pelaajanPisteet.Value += RandomGen.NextInt(200);
        Remove(pelimerkki);
        raha.Play();
    }


    /// <summary>
    /// Aliohjelma poistaa pelaajan, jos se osuu alasimeen.
    /// </summary>
    /// <param name="pelaaja">Pelaajan hahmo.</param>
    /// <param name="alasin">Alasin.</param>
    private void AlasinVsPelaaja(PhysicsObject pelaaja, PhysicsObject alasin)
    {
        player = pelaaja as PlatformCharacter;
        Explosion rajahdys = new Explosion(player.Width * 3);
        rajahdys.Position = player.Position;
        rajahdys.UseShockWave = true;
        this.Add(rajahdys);
        Remove(pelaaja);
        topLista.EnterAndShow(pelaajanPisteet.Value);
        topLista.HighScoreWindow.Closed += Valikko;
    }


    /// <summary>
    /// Aliohjelma kutsuu valikko-aliohjelmaa pelaajan kuolemisen jälkeen.
    /// </summary>
    /// <param name="Sender"></param>
    private void Valikko(Window Sender)
    {
        Valikko();
    }


    /// <summary>
    /// Reunan ja putoavan esineen törmätessä tuhotaan putoava esine.
    /// </summary>
    /// <param name="reuna">Osuttava kohde.</param>
    /// <param name="putoaja">Osuva kohde.</param>
    private void Putoaminen(PhysicsObject reuna, PhysicsObject putoaja)
    {
        putoaja.Destroy();
    }


    /// <summary>
    /// Reunan ja pelaajan törmätessä tuhotaan pelaaja ja lopetetaan peli.
    /// </summary>
    /// <param name="reuna">Osuttava reuna.</param>
    /// <param name="pelaaja">Pelaaja.</param>
    private void PelaajanPutoaminen(PhysicsObject reuna, PhysicsObject pelaaja)
    {
        player = pelaaja as PlatformCharacter;
        Remove(pelaaja);
        topLista.EnterAndShow(pelaajanPisteet.Value);
        topLista.HighScoreWindow.Closed += Valikko;
    }


    /// <summary>
    /// Aliohjelma kutsuu pistelaskurin luovaa aliohjelmaa luomaan laskurin ruudun vasempaan yläreunaan ja asettaa sen alkuarvoksi 0.
    /// </summary>
    private void LisaaPistelaskuri()
    {
        pelaajanPisteet = PisteLaskuri(Screen.Left + 100.0, Screen.Top - 100.0);
        pelaajanPisteet.Value = 0;
    }


    /// <summary>
    /// Aliohjelma luo pistelaskurin.
    /// </summary>
    /// <param name="x">Pistelaskurin x-koordinaatti.</param>
    /// <param name="y">Pistelaskurin y-koordinaatti.</param>
    /// <returns></returns>
    private IntMeter PisteLaskuri(double x, double y)
    {
        IntMeter laskuri = new IntMeter(0);
        laskuri.MaxValue = 10000;

        Label naytto = new Label();
        naytto.BindTo(laskuri);
        naytto.X = x;
        naytto.Y = y;
        naytto.TextColor = Color.DarkAzure;
        naytto.BorderColor = Color.Black;
        naytto.Color = Color.Crimson;
        Add(naytto);

        return laskuri;
    }

}