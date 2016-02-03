using UnityEngine;
using System.Collections;

public class MouseOverFeedbackC : MonoBehaviour {

    public Color offColor = new Color(0.71f, 0.71f, 0.71f, 0.63f);
    public Color onColor = new Color(0.0f, 0.79f, 1.0f, 0.63f);
    public Color clickColor = new Color(1.0f, 0.34f, 0.34f, 1.0f);

    public float clickSpeed = 8.0f;
    public float offSpeed = 4.0f;
    public float onSpeed = 2.0f;

    public float maxScaleSize = 1.2f;

    protected bool shouldUpdate;
    protected float counter;
    protected bool mouseDown = false;
    protected float counterLimit;
    protected float speed;

    protected Material normal;
    public Material hover;
    public Material click;

    /**
     * Scales the attached game object to maxScaleSize on mouseOver at speed onSpeed and changes color to clickColor.
     * When the mouse is removed, it scales it back to 1.0 at offspeed and changes color to offColor.  
     * When it is clicked, it scales it to 1.0 at clickspeed and changes color to clickColor.
     */
	
	void Start(){ DoStart();}
	
    protected virtual void DoStart() 
    {
	    //renderer.material.color = offColor;
        normal = renderer.material;
	    DoReset();
    }
	
	void Update(){ DoUpdate();}

    protected virtual void DoUpdate() 
    {
	    if (shouldUpdate) {
		    if (counter < counterLimit) {
			    counter += Time.deltaTime * speed;
			    if (counter > 3.14) {
				    DoReset();
			    }
		    }
		    float amt = 1.0f + Mathf.Sin(counter) * (maxScaleSize - 1.0f);
		    transform.localScale = new Vector3(amt, amt, amt);
	    }
    }
	
    protected virtual void DoReset() {
	    counter = 0.0f;
	    speed = onSpeed;
	    counterLimit = 1.57f;
	    shouldUpdate = false;
	    transform.localScale = new Vector3(1, 1, 1);
    }

    void OnMouseEnter() {
	    if (!mouseDown) {
		    shouldUpdate = true;
	    }
    }

    void OnMouseOver() {
	    if (!mouseDown) {
            renderer.material = hover;
		    //renderer.material.color = onColor;
		    shouldUpdate = true;
	    } else {
            renderer.material = click;
		    //renderer.material.color = clickColor;
	    }
    }

    void OnMouseDown() {
        renderer.material = click;
	    //renderer.material.color = clickColor;
	    mouseDown = true;
	    counterLimit = 3.14f;
	    speed = clickSpeed;
    }

    void OnMouseUp()
    {
        renderer.material = normal;
	    //renderer.material.color = offColor;
	    mouseDown = false;
	    DoReset();
    }

    void OnMouseExit()
    {
	    counterLimit = 3.14f;
	    speed = offSpeed;
	    if (!mouseDown) {
            renderer.material = normal;
		    //renderer.material.color = offColor;
	    }
    }
}

