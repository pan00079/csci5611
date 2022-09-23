//Simulation-Driven Animation
//CSCI 5611 Example - Bouncing Balls [Exercise]
// Stephen J. Guy <sjguy@umn.edu>

//NOTE: The simulation starts paused! Press space to run it.

//TODO:
//  1. The balls start red, make them blue instead. DONE
//  2. Randomize the initial particle velocities so that xVel starts in the range (30,90),
//     and yVel starts in the range (-190, -200) DONE
//  3. There is currently a small cap on the number of particles that can be spawned. 
//     Raise it up to 400. DONE
//  4. Currently, pressing the 'r' key prints that it's resetting the particle system,
//     but it doesn't actually reset anything yet. Fix that by having the simulation
//     reset when the user presses 'r'. DONE
//  5. Pressing the arrow keys will move the big red ball, but sometimes we would like
//     to move it faster. Adjust the code so that holding 'shift' while an arrow key
//     is pressed will make the red ball move twice as fast. DONE
//  6. The red ball only moves up/down/left/right. Change the code so that is two keys
//     are pressed simultaneously the red ball will move diagonally. DONE
//  7. A common pitfall in games is that the diagonal motion is actually faster than
//     horizontal motion (because you just add the vectors). Make sure your code solving 
//     Step 6 does not have that issue, and that the red ball moves diagonally at the same speed 
//     it move horizontally or vertically. DONE
//  8. The small blue balls (particles) have momentum, but they are missing the effect
//     of acceleration due to gravity. Add gravity to the simulation.  DONE
//     Two things to consider in your implementation:
//        -Check to make sure that your particles move in a smooth, clear parabolic arc.
//        -When choosing the magnitude of your gravity vector think very carefully about
//         what the units mean. Don't just pick 9.8 for gravity unless you are sure it
//         makes sense in the units of the scene you are simulated (hint: how many meters
//         long do you envision a pixel is in your scene).
//  9. The current code for bouncing the particles off of the red ball assumes the red
///    ball is stationary. If the ball is moving (e.g., controlled by the user) it should
//     impart some momentum on the particles. Update the collision response to capture this
//     effect in some way. There is no perfect answer here, but try to find something that
//     looks natural. DONE

//Challenge:
//  1. Delete particles which have been around too long (and allow new ones to be created) DONE?
//  2. Change the color of the particles over time DONE?
//  3. Change the color of the particles as a function of the bounce 



//Simulation paramaters
static int maxParticles = 400;
static int maxLife = 10;
Vec2 spherePos = new Vec2(300,400);
float sphereRadius = 60;
float r = 5;
float genRate = 20;
float obstacleSpeed = 200;
float COR = 0.7;
Vec2 gravity = new Vec2(0,300);

//Initalalize variable
Vec2 pos[] = new Vec2[maxParticles];
Vec2 vel[] = new Vec2[maxParticles];
float life[] = new float[maxParticles]; // keeps track of how long the particles are alive
int numParticles = 0;

void setup(){
  size(640,480);
  surface.setTitle("Particle System [CSCI 5611 Example]");
  strokeWeight(2); //Draw thicker lines 
}

Vec2 obstacleVel = new Vec2(0,0);

void update(float dt){
  float toGen_float = genRate * dt;
  int toGen = int(toGen_float);
  float fractPart = toGen_float - toGen;
  if (random(1) < fractPart) toGen += 1;
  for (int i = 0; i < toGen; i++){
    if (numParticles >= maxParticles) break;
    pos[numParticles] = new Vec2(20+random(20),200+random(20));
    int randXVel = int(random(30,90));
    int randYVel = int(random(-190, -200));
    vel[numParticles] = new Vec2(randXVel, randYVel); 
    numParticles += 1;
  }
  
  obstacleVel = new Vec2(0,0);
  if (leftPressed) obstacleVel.add(new Vec2(-obstacleSpeed,0));
  if (rightPressed) obstacleVel.add(new Vec2(obstacleSpeed,0));
  if (upPressed) obstacleVel.add(new Vec2(0,-obstacleSpeed));
  if (downPressed) obstacleVel.add(new Vec2(0,obstacleSpeed));
  
  obstacleVel.clampToLength(obstacleSpeed);
  
  if (shiftPressed) obstacleVel.mul(2);
  spherePos.add(obstacleVel.times(dt));
  
  for (int i = 0; i <  numParticles; i++){
    
    if (life[i] > maxLife) {
      pos[i] = new Vec2(20+random(20),200+random(20));
      int randXVel = int(random(30,90));
      int randYVel = int(random(-190, -200));
      vel[i] = new Vec2(randXVel, randYVel); 
      life[i] = 0;
    }
    
    Vec2 acc = gravity; //Gravity
    
    pos[i].add(vel[i].times(dt)); //Update position based on velocity
    vel[i].add(acc.times(dt));
    life[i] += dt;
    
    if (pos[i].y > height - r){
      pos[i].y = height - r;
      vel[i].y *= -COR;
    }
    if (pos[i].y < r){
      pos[i].y = r;
      vel[i].y *= -COR;
    }
    if (pos[i].x > width - r){
      pos[i].x = width - r;
      vel[i].x *= -COR;
    }
    if (pos[i].x < r){
      pos[i].x = r;
      vel[i].x *= -COR;
    }
    
    if (pos[i].distanceTo(spherePos) < (sphereRadius+r)){
      Vec2 normal = (pos[i].minus(spherePos)).normalized();
      pos[i] = spherePos.plus(normal.times(sphereRadius+r).times(1.01));
      Vec2 velNormal = normal.times(dot(vel[i],normal));
      vel[i].subtract(velNormal.times(1 + COR));
      vel[i].add(projAB(obstacleVel, normal));
      
    }
  }
  
}

boolean leftPressed, rightPressed, upPressed, downPressed, shiftPressed;
void keyPressed(){
  if (keyCode == LEFT) leftPressed = true;
  if (keyCode == RIGHT) rightPressed = true;
  if (keyCode == UP) upPressed = true; 
  if (keyCode == DOWN) downPressed = true;
  if (keyCode == SHIFT) shiftPressed = true;
  if (key == ' ') paused = !paused;
}

void keyReleased(){
  if (key == 'r'){
    println("Reseting the System");
    for (int i = 0; i < maxParticles; i++) {
      pos[i] = null;
      vel[i] = null;
      life[i] = 0;
    }     
    numParticles = 0;
  }
  if (keyCode == LEFT) leftPressed = false;
  if (keyCode == RIGHT) rightPressed = false;
  if (keyCode == UP) upPressed = false; 
  if (keyCode == DOWN) downPressed = false;
  if (keyCode == SHIFT) shiftPressed = false;
}


boolean paused = true;
void draw(){
  if (!paused) update(1.0/frameRate);
  
  background(255); //White background
  //fill(20,120,20);
  for (int i = 0; i < numParticles; i++){
    float col = life[i] * 300 / maxLife;
    stroke(0,0,0, 255-col);
    fill(50, 75+col, 100+col, 255-col);
    circle(pos[i].x, pos[i].y, r*2); //(x, y, diameter)
  }
  
  stroke(0,0,0);
  fill(40,60,180);
  circle(spherePos.x, spherePos.y, sphereRadius*2); //(x, y, diameter)
}

// Begin the Vec2 Libraray

//Vector Library
//CSCI 5611 Vector 2 Library [Example]
// Stephen J. Guy <sjguy@umn.edu>

public class Vec2 {
  public float x, y;
  
  public Vec2(float x, float y){
    this.x = x;
    this.y = y;
  }
  
  public String toString(){
    return "(" + x+ "," + y +")";
  }
  
  public float length(){
    return sqrt(x*x+y*y);
  }
  
  public Vec2 plus(Vec2 rhs){
    return new Vec2(x+rhs.x, y+rhs.y);
  }
  
  public void add(Vec2 rhs){
    x += rhs.x;
    y += rhs.y;
  }
  
  public Vec2 minus(Vec2 rhs){
    return new Vec2(x-rhs.x, y-rhs.y);
  }
  
  public void subtract(Vec2 rhs){
    x -= rhs.x;
    y -= rhs.y;
  }
  
  public Vec2 times(float rhs){
    return new Vec2(x*rhs, y*rhs);
  }
  
  public void mul(float rhs){
    x *= rhs;
    y *= rhs;
  }
  
  public void clampToLength(float maxL){
    float magnitude = sqrt(x*x + y*y);
    if (magnitude > maxL){
      x *= maxL/magnitude;
      y *= maxL/magnitude;
    }
  }
  
  public void setToLength(float newL){
    float magnitude = sqrt(x*x + y*y);
    x *= newL/magnitude;
    y *= newL/magnitude;
  }
  
  public void normalize(){
    float magnitude = sqrt(x*x + y*y);
    x /= magnitude;
    y /= magnitude;
  }
  
  public Vec2 normalized(){
    float magnitude = sqrt(x*x + y*y);
    return new Vec2(x/magnitude, y/magnitude);
  }
  
  public float distanceTo(Vec2 rhs){
    float dx = rhs.x - x;
    float dy = rhs.y - y;
    return sqrt(dx*dx + dy*dy);
  }
}

Vec2 interpolate(Vec2 a, Vec2 b, float t){
  return a.plus((b.minus(a)).times(t));
}

float interpolate(float a, float b, float t){
  return a + ((b-a)*t);
}

float dot(Vec2 a, Vec2 b){
  return a.x*b.x + a.y*b.y;
}

Vec2 projAB(Vec2 a, Vec2 b){
  return b.times(a.x*b.x + a.y*b.y);
}
